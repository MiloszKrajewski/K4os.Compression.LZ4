open System.Net
open Fake

#r ".fake/FakeLib.dll"
#load "build.tools.fsx"

let download fn (url: string) = 
    if File.exists fn |> not then 
        printfn "Downloading: %s" url
        use wc = new WebClient() in wc.DownloadFile(url, fn)

let clean () = !! "**/bin/" ++ "**/obj/" |> DeleteDirs
let build () = Proj.build "src/K4os.Compression.LZ4.sln"
let restore () = Proj.restore "src/K4os.Compression.LZ4.sln"
let test () = Proj.xtestAll ()

let release () = Proj.releaseNupkg ()

Target "Clean" (fun _ -> clean ())

Target "Restore" (fun _ -> restore ())

Target "Build" (fun _ -> build ())

Target "Rebuild" ignore

Target "Release" (fun _ -> release ())

Target "Test" (fun _ -> test ())

Target "Release:Nuget" (fun _ ->
    let apiKey = Proj.settings |> Config.valueOrFail "nuget" "accessKey"
    Proj.publishNugetOrg apiKey "K4os.Compression.LZ4"
)

let uncorpus fn (uri: string) = 
    let fn = sprintf "./.corpus/%s" fn
    if not (File.exists fn) then
        if not (File.exists "./.tools/7za.exe") then
            CreateDir "./.tools"
            download "./.tools/7za920.zip" "http://www.7-zip.org/a/7za920.zip"
            ZipHelper.Unzip "./.tools/" "./.tools/7za920.zip"
        fn |> directory |> CreateDir
        let bz2 = sprintf "%s.bz2" fn
        download bz2 uri
        Shell.run ".\\.tools\\7za.exe" (sprintf "-o%s x %s" (directory bz2) bz2)

Target "Restore:Corpus" (fun _ ->
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