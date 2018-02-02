#r ".fake/FakeLib.dll"
#load "build.tools.fsx"

open Fake

let clean () = !! "**/bin/" ++ "**/obj/" |> DeleteDirs
let build () = Proj.build "src"
let restore () = Proj.restore "src"
let test () = Proj.testAll ()
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

"Restore" ==> "Build"
"Build" ==> "Rebuild"
"Clean" ?=> "Restore"
"Clean" ==> "Rebuild"
"Rebuild" ==> "Release"
"Test" ==> "Release"
"Build" ?=> "Test"
"Release" ==> "Release:Nuget"

RunTargetOrDefault "Build"