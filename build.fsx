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

let uncorpus fn (uri: string) =
    let dataFile = sprintf "./.corpus/%s" fn
    if not (File.exists dataFile) then
        dataFile |> directory |> CreateDir
        let bz2File = sprintf "%s.bz2" dataFile
        File.download bz2File uri
        Shell.run ".\\.tools\\7za.exe" (sprintf "-o%s x %s" (directory bz2File) bz2File)
        DeleteFile bz2File

Target "Restore:Corpus" (fun _ ->
    enusure7Zexe ()
    ensureLZ4exe ()
    uncorpus "dickens" "http://sun.aei.polsl.pl/~sdeor/corpus/dickens.bz2"
    uncorpus "mozilla" "http://sun.aei.polsl.pl/~sdeor/corpus/mozilla.bz2"
    uncorpus "mr" "http://sun.aei.polsl.pl/~sdeor/corpus/mr.bz2"
    uncorpus "nci" "http://sun.aei.polsl.pl/~sdeor/corpus/nci.bz2"
    uncorpus "ooffice" "http://sun.aei.polsl.pl/~sdeor/corpus/ooffice.bz2"
    uncorpus "osdb" "http://sun.aei.polsl.pl/~sdeor/corpus/osdb.bz2"
    uncorpus "reymont" "http://sun.aei.polsl.pl/~sdeor/corpus/reymont.bz2"
    uncorpus "samba" "http://sun.aei.polsl.pl/~sdeor/corpus/samba.bz2"
    uncorpus "sao" "http://sun.aei.polsl.pl/~sdeor/corpus/sao.bz2"
    uncorpus "webster" "http://sun.aei.polsl.pl/~sdeor/corpus/webster.bz2"
    uncorpus "xml" "http://sun.aei.polsl.pl/~sdeor/corpus/xml.bz2"
    uncorpus "x-ray" "http://sun.aei.polsl.pl/~sdeor/corpus/x-ray.bz2"
    
)

"Restore:Corpus" ==> "Restore" ==> "Build" ==> "Rebuild" ==> "Test" ==> "Release" ==> "Release:Nuget"
"Clean" ==> "Rebuild"
"Clean" ?=> "Restore"
"Build" ?=> "Test"


RunTargetOrDefault "Build"