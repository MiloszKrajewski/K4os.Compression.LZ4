#r "paket:
	nuget Fake.Core.Target
	nuget Fake.Core.ReleaseNotes
	nuget Fake.IO.FileSystem
	nuget Fake.IO.Zip
	nuget Fake.Api.GitHub
	nuget Fake.DotNet.MSBuild
	nuget Fake.DotNet.Cli
	nuget Fake.DotNet.Testing.XUnit2
	nuget Octokit 0.48
//"

#load "build.imports.fsx"
#load "build.tools.fsx"
#load "sanitize.fsx"

open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators
open Fake.Core
open Fake.Api

open Tools

let solutions = Proj.settings |> Config.keys "Build"
let packages = Proj.settings |> Config.keys "Pack"

let clean () = !! "**/bin/" ++ "**/obj/" |> Shell.deleteDirs
let build () = solutions |> Proj.buildMany
let restore () = solutions |> Proj.restoreMany
let test () = Proj.testAll ()
let release () = packages |> Proj.packMany
let publish apiKey = packages |> Seq.iter (Proj.publishNugetOrg apiKey)

Target.create "Refresh" (fun _ ->
    Proj.regenerateStrongName "K4os.snk"
    Proj.updateCommonTargets "Common.targets"
)

Target.create "Clean" (fun _ -> clean ())

Target.create "Restore" (fun _ -> restore ())

Target.create "Preprocess" (fun _ ->
    let preprocess defines transforms (source, target) =
        Trace.logfn "%s -> %s" source target
        [
            "//------------------------------------------------------------------------------\r\n//"
            "// This file has been generated. All changes will be lost."
            "//\r\n//------------------------------------------------------------------------------"
            for d in defines do sprintf "#define %s" d
            ""
            source |> File.loadText
        ]
        |> String.concat "\r\n"
        |> transforms
        |> File.saveText target

    let root = "./src/K4os.Compression.LZ4"
    !! (root @@ "**/x64/*64*.cs")
    |> Seq.map (fun fn -> fn, String.replace "64" "32" fn)
    |> Seq.iter (preprocess ["BIT32"] id)
    
    let deasync =
        Sanitizer.replaceText "async" "/*async*/" >>
        Sanitizer.replaceText "await" "/*await*/" >>
        Sanitizer.replaceExpr "Task[<](?<type>[A-Za-z0-9_]+[?]?)[>]" (fun g -> g "type") >>
        Sanitizer.replaceText "Task" "void" >>
        Sanitizer.replaceRaw "\\s*\\.Weave\\(\\)" "" >>
        id
    
    let root = "./src/K4os.Compression.LZ4.Streams"
    !! (root @@ "**/*.async.cs")
    |> Seq.map (fun fn -> fn, String.replace ".async." ".blocking." fn)
    |> Seq.iter (preprocess ["BLOCKING"] deasync)
)

Target.create "Build" (fun _ -> build ())

Target.create "Rebuild" ignore

Target.create "Release" (fun _ -> release ())

Target.create "Test" (fun p ->
    if p.Context.Arguments |> List.contains "notest"
    then Log.warn "Ignoring tests"
    else test ()
)

Target.create "Benchmark" (fun _ ->
    Environment.environVarOrDefault "args" ""
    |> sprintf "run -p src/K4os.Compression.LZ4.Benchmarks -c Release -- %s"
    |> Shell.run "dotnet"
)

Target.create "Release:Nuget" (fun _ ->
    Proj.settings |> Config.valueOrFail "nuget" "accessKey" |> publish
)

Target.create "Release:GitHub" (fun _ ->
    let user = Proj.settings |> Config.valueOrFail "github" "user"
    let token = Proj.settings |> Config.valueOrFail "github" "token"
    let repository = Proj.settings |> Config.keys "Repository" |> Seq.exactlyOne

    !! (Proj.outputFolder @@ (sprintf "*.%s.nupkg" Proj.productVersion))
    |> Proj.publishGitHub repository user token
)

Target.create "Sanitize" (fun _ ->
    let rules = Sanitizer.basicTypes
    let sanitize fn =
        printfn "Processing: %s" fn
        Sanitizer.sanitize (sprintf "./orig/lib/%s" fn) (sprintf "./src/sanitized/%s" fn) rules
    sanitize "lz4.c"
    sanitize "lz4hc.c"
    sanitize "lz4.h"
    sanitize "lz4hc.h"
)

let enusure7Zexe () =
    if not (File.exists "./.tools/7za.exe") then
        let zipFileUrl = "http://www.7-zip.org/a/7za920.zip"
        let zipFileName = "./.tools/7za920.zip"
        "./.tools" |> Directory.create
        zipFileUrl |> File.download zipFileName
        zipFileName |> Zip.unzip "./.tools/"

let ensureLZ4exe () =
    if not (File.exists "./.tools/lz4.exe") then
        let zipFileUrl = "https://github.com/lz4/lz4/releases/download/v1.8.1.2/lz4_v1_8_1_win64.zip"
        let zipFile = "./.tools/lz4-1.8.1-win64.zip"
        "./.tools" |> Directory.create
        zipFileUrl |> File.download zipFile
        zipFile |> Zip.unzip "./.tools/lz4"
        "./.tools/lz4/lz4.exe" |> File.copy "./.tools/lz4.exe"
        "./.tools/lz4" |> Directory.delete

let uncorpus fn =
    let dataFile = sprintf "./.corpus/%s" fn
    if not (File.exists dataFile) then
        let corpusUrl = sprintf "https://github.com/MiloszKrajewski/SilesiaCorpus/blob/master/%s.zip?raw=true" fn
        dataFile |> Path.dirnameOf |> Directory.create
        let zipFile = sprintf "%s.zip" dataFile
        corpusUrl |> File.download zipFile
        Shell.run ".\\.tools\\7za.exe" (sprintf "-o%s x %s" (Path.dirnameOf zipFile) zipFile)
        zipFile |> File.delete

Target.create "Restore:Corpus" (fun _ ->
    enusure7Zexe ()
    ensureLZ4exe ()
    uncorpus "dickens"
    uncorpus "mozilla"
    uncorpus "mr"
    uncorpus "nci"
    uncorpus "ooffice"
    uncorpus "osdb"
    uncorpus "reymont"
    uncorpus "samba"
    uncorpus "sao"
    uncorpus "webster"
    uncorpus "xml"
    uncorpus "x-ray"
)

open Fake.Core.TargetOperators

"Restore:Corpus" ==> "Preprocess" ==> "Refresh" ==> "Restore" ==> "Build" ==> "Rebuild" ==> "Release"
// "Release" ==> "Release:GitHub" ==> "Release:Nuget"
"Clean" ?=> "Restore"
"Clean" ==> "Rebuild"
"Build" ?=> "Test"

Target.runOrDefaultWithArguments "Build"
