namespace FsBuildTools

open System
open System.IO
open System.Net
open System.Text.RegularExpressions
open System.Diagnostics

open Fake.Core
open Fake.IO
open Fake.Net

[<AutoOpen>]
module Fx =
    let inline tap f a = f a; a
    let inline forgive f a = try f a with | _ -> ()

module String =
    let quote (arg: string) = if arg.Contains(" ") then $"\"%s{arg}\"" else arg
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
            Log.warn $"File %s{inputFile} has been modified. Overwriting."
            outputFile |> Shell.rename (inputFile |> tap File.delete)

    let exists filename = File.Exists(filename)
    let loadText filename = File.ReadAllText(filename)
    let saveText filename text = File.WriteAllText(filename, text)
    let loadLines filename = File.ReadAllLines(filename)
    let saveLines filename lines = File.WriteAllLines(filename, lines)
    let appendText filename text = File.AppendAllText(filename, text)
    let appendLines filename lines = File.AppendAllLines(filename, lines)

    let touch filename =
        if (exists filename)
        then FileInfo(filename).LastWriteTimeUtc <- DateTime.UtcNow
        else saveText filename ""

    let download filename (url: string) =
        if (exists filename)
        then Log.info $"Local file %s{filename} already exists. Skipping download."
        else Http.downloadFile filename url |> ignore

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
        let command = $"%s{String.quote executable} %s{arguments}"
        Log.info $"> %s{command} @ %s{Path.toRelativeFromCurrent directory}"
        let comspec, comspecArgs = if mono then "bash", "-c" else Environment.environVarOrFail "COMSPEC", "/c"
        let info = ProcessStartInfo(comspec, $"%s{comspecArgs} \"%s{command}\"", UseShellExecute = false, WorkingDirectory = directory)
        let proc = Process.Start(info)
        proc.WaitForExit()
        match proc.ExitCode with | 0 -> () | c -> failwithf $"Execution failed with error code %d{c}"

    let run executable arguments = runAt "." executable arguments

