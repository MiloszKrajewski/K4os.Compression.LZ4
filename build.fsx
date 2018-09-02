open Fake

#r ".fake/FakeLib.dll"
#load "build.tools.fsx"
#load "sanitize.fsx"

let solutions = Proj.settings |> Config.keys "Build"
let packages = Proj.settings |> Config.keys "Pack"

//----

let clean () = !! "**/bin/" ++ "**/obj/" |> DeleteDirs
let restore () = solutions |> Seq.iter Proj.restore
let build () = solutions |> Seq.iter Proj.build
let test () = Proj.xtestAll ()
let release () = packages |> Proj.packMany
let publish apiKey = packages |> Seq.iter (Proj.publishNugetOrg apiKey)

//----

Target "Clean" (fun _ -> clean ())

Target "Restore" (fun _ -> restore ())

Target "Build" (fun _ -> build ())

Target "Rebuild" ignore

Target "Release" (fun _ -> release ())

Target "Test" (fun _ -> test ())

Target "Benchmark" (fun _ ->
    getBuildParamOrDefault "args" ""
    |> sprintf "run -p src/K4os.Compression.LZ4.Benchmarks -c Release -- %s"
    |> Shell.run "dotnet"
)

Target "Release:Nuget" (fun _ -> Proj.settings |> Config.valueOrFail "nuget" "accessKey" |> publish)

Target "Sanitize" (fun _ ->
    let rules = Sanitizer.basicTypes
    let sanitize fn =
        printfn "Processing: %s" fn
        Sanitizer.sanitize (sprintf "./orig/lib/%s" fn) (sprintf "./src/sanitized/%s" fn) rules
    sanitize "lz4.c"
    sanitize "lz4hc.c"
    sanitize "lz4opt.h"
    sanitize "lz4frame.c"
)

let enusure7Zexe () = 
    if not (File.exists "./.tools/7za.exe") then
        let zipFileUrl = "http://www.7-zip.org/a/7za920.zip"
        let zipFileName = "./.tools/7za920.zip"
        CreateDir "./.tools"
        File.download zipFileName zipFileUrl
        ZipHelper.Unzip "./.tools/" zipFileName


let ensureLZ4exe () =
    if not (File.exists "./.tools/lz4.exe") then
        let zipFileUrl = "https://github.com/lz4/lz4/releases/download/v1.8.1.2/lz4_v1_8_1_win64.zip"
        let zipFile = "./.tools/lz4-1.8.1-win64.zip"
        CreateDir "./.tools"
        File.download zipFile zipFileUrl
        ZipHelper.Unzip "./.tools/lz4" zipFile
        "./.tools/lz4/lz4.exe" |> CopyFile "./.tools/lz4.exe"
        DeleteDir "./.tools/lz4"

let uncorpus fn =
    let dataFile = sprintf "./.corpus/%s" fn
    if not (File.exists dataFile) then
        let uri = sprintf "https://github.com/MiloszKrajewski/SilesiaCorpus/blob/master/%s.zip?raw=true" fn
        dataFile |> directory |> CreateDir
        let zipFile = sprintf "%s.zip" dataFile
        File.download zipFile uri
        Shell.run ".\\.tools\\7za.exe" (sprintf "-o%s x %s" (directory zipFile) zipFile)
        DeleteFile zipFile

Target "Restore:Corpus" (fun _ ->
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

"Restore:Corpus" ==> "Restore" ==> "Build" ==> "Rebuild" ==> "Test" ==> "Release" ==> "Release:Nuget"
"Clean" ==> "Rebuild"
"Clean" ?=> "Restore"
"Build" ?=> "Test"


RunTargetOrDefault "Build"