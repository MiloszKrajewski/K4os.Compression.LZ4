#load "build.imports.fsx"

#nowarn "52"

namespace Tools

open System
open System.IO
open System.Net
open System.Text.RegularExpressions
open System.Diagnostics

open Fake.Core
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators
open Fake.DotNet
open Fake.Api

[<AutoOpen>]
module Fx =
    let inline tap f a = f a; a
    let inline forgive f a = try f a with | _ -> ()

module String =
    let quote (arg: string) = if arg.Contains(" ") then sprintf "\"%s\"" arg else arg
    let replace pattern (replacement: string) (input: string) = input.Replace(pattern, replacement)
    let same textA textB = String.Compare(textA, textB, true) = 0

module Option =
    let inline def v o = defaultArg o v
    let inline alt a o = match o with | None -> a | _ -> o
    let inline altGet a o = match o with | None -> a () | _ -> o
    let inline cast<'a> (o: obj) = match o with | :? 'a as v -> Some v | _ -> None
    let inline fromRef o = match o with | null -> None | _ -> Some o

module Log =
    let trace = Trace.logfn
    let debug = Trace.logfn
    let info = Trace.tracefn
    let warn = Trace.traceImportantfn
    let error = Trace.traceErrorfn

module Path =
    let corenameOf (path: string) = Path.GetFileNameWithoutExtension(path)
    let filenameOf (path: string) = Path.GetFileName(path)
    let fullnameOf (path: string) = Path.GetFullPath(path)
    let dirnameOf (path: string) = Path.GetDirectoryName(path)

module File =
    ServicePointManager.SecurityProtocol <-
        ServicePointManager.SecurityProtocol
        ||| SecurityProtocolType.Tls
        ||| SecurityProtocolType.Tls11
        ||| SecurityProtocolType.Tls12

    let copy target source = Shell.copyFile target source
    let rename target source = Shell.rename target source

    let update modifier inputFile =
        let outputFile = Path.GetTempFileName()
        inputFile |> Shell.copyFile outputFile
        modifier outputFile
        if Shell.compareFiles true inputFile outputFile |> not then
            Log.warn "File %s has been modified. Overwriting." inputFile
            outputFile |> Shell.rename (inputFile |> tap File.delete)

    let exists filename = File.Exists(filename)
    let loadText filename = File.ReadAllText(filename)
    let saveText filename text = File.WriteAllText(filename, text)
    let loadLines filename = File.ReadAllLines(filename)
    let saveLines filename lines = File.WriteAllLines(filename, lines)
    let appendText filename text = File.AppendAllText(filename, text)
    let appendLines filename lines = File.AppendAllLines(filename, lines)

    let touch filename =
        if File.Exists(filename)
        then FileInfo(filename).LastWriteTimeUtc <- DateTime.UtcNow
        else saveText filename ""

    let download filename (url: string) =
        if not (exists filename) then
            Log.info "> download %s" url
            use wc = new WebClient() in wc.DownloadFile(url, filename)

module Regex =
    let create ignoreCase pattern =
        let ignoreCase = if ignoreCase then RegexOptions.IgnoreCase else RegexOptions.None
        Regex(pattern, RegexOptions.ExplicitCapture ||| RegexOptions.IgnorePatternWhitespace ||| ignoreCase)
    let replace pattern (replacement: string) input = Regex.Replace(input, pattern, replacement)
    let matches pattern input = Regex.IsMatch(input, pattern)
    let (|Match|_|) (pattern: Regex) text =
        match pattern.Match(text) with | m when m.Success -> Some m | _ -> None

module Shell =
    let mono = "Mono.Runtime" |> Type.GetType |> isNull |> not
    let delete filename = File.delete filename
    let deleteAll filenames = File.deleteAll filenames
    let runAt directory executable arguments =
        let command = sprintf "%s %s" (String.quote executable) arguments
        Log.info "> %s @ %s" command (Path.toRelativeFromCurrent directory)
        let comspec, comspecArgs = if mono then "bash", "-c" else Environment.environVarOrFail "COMSPEC", "/c"
        let info = ProcessStartInfo(comspec, (comspecArgs, command) ||> sprintf "%s \"%s\"", UseShellExecute = false, WorkingDirectory = directory)
        let proc = Process.Start(info)
        proc.WaitForExit()
        match proc.ExitCode with | 0 -> () | c -> failwithf "Execution failed with error code %d" c

    let run executable arguments = runAt "." executable arguments

module Config =
    type Item = { Section: string; Key: string; Value: string }
    let private sectionRx = """^\s*\[\s*(?<name>.*?)\s*\]\s*$""" |> Regex.create true
    let private valueRx = """^\s*(?<key>.*?)\s*(=\s*(?<value>.*?)\s*)?$""" |> Regex.create true
    let private emptyRx = """^\s*(;.*)?$""" |> Regex.create true
    let validate (items: Item seq) = items |> Seq.distinctBy (fun i -> i.Section, i.Key) |> List.ofSeq
    let merge configs = configs |> Seq.collect id |> validate
    let load (lines: seq<string>) =
        let rec parse lines section result =
            match lines with
            | [] -> result
            | line :: lines ->
                match line with
                | Regex.Match emptyRx _ -> parse lines section result
                | Regex.Match sectionRx m ->
                    let section = m.Groups.["name"].Value
                    parse lines section result
                | Regex.Match valueRx m ->
                    let key = m.Groups.["key"].Value
                    let value = match m.Groups.["value"] with | m when m.Success -> m.Value | _ -> ""
                    let item = { Section = section; Key = key; Value = value }
                    parse lines section (item :: result)
                | _ ->
                    Log.error "Line '%s' does not match config pattern as has been ignored" line
                    parse lines section result
        parse (lines |> List.ofSeq) "" [] |> List.rev |> validate

    let tryLoadFile fileName = if File.Exists(fileName) then fileName |> File.read |> load else []
    let items section (config: Item seq) = config |> Seq.filter (fun i -> i.Section = section)
    let keys section (config: Item seq) = config |> items section |> Seq.map (fun i -> i.Key)
    let value section key (config: Item seq) =
        config |> items section |> Seq.filter (fun i -> i.Key = key) |> Seq.tryHead |> Option.map (fun i -> i.Value)
    let valueOrDefault section key defaultValue config =
        config |> value section key |> Option.def defaultValue
    let valueOrFail section key config =
        match config |> value section key with
        | Some v -> v | None -> failwithf "Value %s:%s could not be found" section key

module Proj =
    let outputFolder = ".output"
    let releaseNotes = "./CHANGES.md" |> ReleaseNotes.load
    let timestamp =
        let baseline = DateTime.Parse("2000-01-01T00:00:00") // ~y2k
        DateTime.Now.Subtract(baseline).TotalSeconds |> int |> sprintf "%8x"
    let productVersion = releaseNotes.NugetVersion |> Regex.replace "-wip$" (timestamp |> sprintf "-wip%s")
    let assemblyVersion = releaseNotes.AssemblyVersion
    let isPreRelease = releaseNotes.SemVer.PreRelease.IsSome
    let settings =
        ["."]
        |> Seq.collect (fun dn -> ["settings"; ".secrets"] |> Seq.map (fun fn -> dn @@ fn))
        |> Seq.collect (fun fn -> [".ini"; ".cfg"] |> Seq.map (fun ext -> sprintf "%s%s" fn ext))
        |> Seq.map Config.tryLoadFile
        |> Config.merge
    let listProj () =
        let isValidProject projectPath =
            let projectFolder = projectPath |> Path.dirnameOf |> Path.filenameOf
            let projectName = projectPath |> Path.corenameOf
            String.same projectFolder projectName
        !! "src/*/*.??proj" |> Seq.filter isValidProject

    let findSln name = !! (sprintf "src/%s.sln" name)
    let findProj name = !! (sprintf "src/%s/%s.??proj" name name)

    let isTestProj projectFile = projectFile |> Path.dirnameOf |> Regex.matches "Test$"

    let updateVersions projectFile =
        projectFile |> File.update (fun fn ->
            Xml.pokeInnerText fn "/Project/PropertyGroup/Version" productVersion
            Xml.pokeInnerText fn "/Project/PropertyGroup/AssemblyVersion" assemblyVersion
            Xml.pokeInnerText fn "/Project/PropertyGroup/FileVersion" assemblyVersion
        )

    let restore solution =
        solution |> findSln |> Seq.iter (DotNet.restore id)
    let restoreMany solutions =
        solutions |> Seq.iter restore

    let build solution =
        solution |> findSln |> Seq.iter (DotNet.build (fun p ->
            { p with
                // NoRestore = true
                Configuration = DotNet.Release
            }))
    let buildMany solutions =
        solutions |> Seq.iter build

    let test project =
        project |> DotNet.test (fun p ->
            { p with
                NoBuild = false
                NoRestore = false
                Configuration = DotNet.Release
                Common = { p.Common with Verbosity = Some DotNet.Normal }
            })
    let testMany projects =
        projects |> Seq.iter test
    let testAll () =
        listProj () |> Seq.filter isTestProj |> testMany

    let regenerateStrongName filename =
        let sn = !! "C:/Program Files (x86)/Microsoft SDKs/Windows/**/bin/**/sn.exe" |> Seq.tryHead
        match File.exists filename, sn with
        | true, _ -> ()
        | _, Some sn -> filename |> String.quote |> sprintf "-k %s" |> Shell.run sn
        | _ -> failwith "SN.exe could not be found"

    let updateCommonTargets filename =
        updateVersions filename

    let pack version project =
        project |> DotNet.pack (fun p ->
            { p with
                // NoBuild = true
                // NoRestore = true
                Configuration = DotNet.Release
                OutputPath = outputFolder |> Path.getFullName |> Some
            })
        let versionFile = outputFolder @@ (Path.filenameOf project) |> sprintf "%s.nupkg.latest"
        version |> File.saveText versionFile
    let publish targetFolder project =
        project |> DotNet.publish (fun p ->
            { p with
                // NoBuild = true
                // NoRestore = true
                Configuration = DotNet.Release
                OutputPath = targetFolder |> Path.getFullName |> Some
                Common = { p.Common with CustomParams = Some "/p:BuildProjectReferences=false" }
            })
    let publishNugetOrg accessKey project =
        let version = outputFolder @@ project + ".nupkg.latest" |> File.read |> Seq.head
        let nupkg = project + "." + version + ".nupkg"
        let args = sprintf "nuget push -s https://www.nuget.org/api/v2/package %s -k %s" nupkg accessKey
        Shell.runAt outputFolder "dotnet" args

    let publishGitHub repository user token files =
        let notes = releaseNotes.Notes
        let prerelease = releaseNotes.SemVer.PreRelease.IsSome

        GitHub.createClientWithToken token
        |> GitHub.draftNewRelease user repository productVersion prerelease notes
        |> GitHub.uploadFiles files
        |> GitHub.publishDraft
        |> Async.RunSynchronously

    let fixPackReferences folder =
        let fileMissing filename = File.Exists(filename) |> not
        !! (folder @@ "**/paket.references")
        |> Seq.map Path.dirnameOf
        |> Seq.iter (fun projectPath ->
            let projectName = projectPath |> Path.filenameOf
            !! (projectPath @@ (projectName |> sprintf "%s.*proj"))
            |> Seq.iter (fun projectFile ->
                let referenceFile = projectPath @@ "obj" @@ (Path.filenameOf projectFile) + ".references"
                let nuspecFile = projectPath @@ "obj" @@ projectName + "." + productVersion + ".nuspec"
                // recreate 'projectName.csproj.refereces' even if it is empty
                if referenceFile |> fileMissing then
                    referenceFile
                    |> tap (Log.warn "Creating: %s")
                    |> tap (Path.dirnameOf >> Directory.create)
                    |> File.touch
                // delete 'old' .nuspec (they are messing with pack)
                !! (projectPath @@ "obj" @@ "*.nuspec") -- nuspecFile |> File.deleteAll
            )
        )
    let packMany projects =
        let projectFiles = projects |> Seq.map (fun p -> sprintf "src/%s/%s.*proj" p p) |> Seq.collect (!!) |> Seq.toArray
        let folders = projectFiles |> Seq.map Path.dirnameOf |> Seq.distinct |> Seq.toArray
        folders |> Seq.iter (fun folder ->
            folders |> Seq.iter fixPackReferences
            pack productVersion folder
        )
