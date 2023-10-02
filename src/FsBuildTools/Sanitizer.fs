namespace FsBuildTools

open System.IO
open System.Text.RegularExpressions

open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

module Sanitizer =
    let private b = @"(\b|\s|^|$|[\(\)\[\]])"
    let private sanitize source target rules =
        File.WriteAllText (target, rules (File.ReadAllText source))
    let replaceRaw pattern (value: string) content =
        Regex.Replace(content, pattern, value)
    let replaceText pattern (value: string) content =
        Regex.Replace(content, sprintf @"(?<=%s)(%s)(?=%s)" b pattern b, value)
    let replaceExpr pattern evaluator content =
        let evaluator = MatchEvaluator(fun m -> evaluator (fun (n: string) -> m.Groups.[n].Value))
        Regex.Replace(content, sprintf @"(?<=%s)(%s)(?=%s)" b pattern b, evaluator)
    let rec repeat replacer content =
        match replacer content with
        | c when c = content -> content
        | c -> repeat replacer c

    let private basicTypes =
        replaceText @"NULL" "null" >>
        replaceText @"char" "byte" >>
        replaceText @"BYTE" "byte" >>
        replaceText @"U8" "byte" >>
        replaceText @"U16" "ushort" >>
        replaceText @"U32" "uint" >>
        replaceText @"S32" "int" >>
        replaceText @"U64" "ulong" >>
        replaceText @"uint8_t" "byte" >>
        replaceText @"uint16_t" "ushort" >>
        replaceText @"uint32_t" "uint" >>
        replaceText @"uint64_t" "ulong" >>
        replaceText @"uptrval" "uptr_t" >>
        replaceText @"reg_t" "ureg_t" >>
        replaceText @"unsigned" "uint" >>
        replaceText @"base" "@base" >>
        replaceText @"ref" "@ref" >>
        replaceText @"LZ4_writeLE16" "Mem.Poke2" >>
        replaceText @"LZ4_read16" "Mem.Peek2" >>
        replaceText @"LZ4_read32" "Mem.Peek4" >>
        replaceText @"LZ4_write32" "Mem.Poke4" >>
        replaceText @"LZ4_read_ARCH" "Mem.PeekW" >>
        replaceText @"LZ4_wildCopy8" "Mem.WildCopy8" >>
        replaceText @"memcpy" "Mem.Copy" >>
        replaceText @"memmove" "Mem.Move" >>
        replaceText @"assert" "Assert" >>
        replaceExpr @"const\s+(?<type>\w+)\s*\*\s+const" (fun g -> g "type" |> sprintf "%s*") >>
        replaceExpr @"const\s+(?<type>\w+)\s*\*" (fun g -> g "type" |> sprintf "%s*") >>
        replaceExpr @"(?<type>\w+)\s*\*\s+const" (fun g -> g "type" |> sprintf "%s*") >>
        replaceExpr @"(?<type>\w+)\s+const" (fun g -> g "type") >>
        replaceExpr @"const\s+(?<type>\w+)" (fun g -> g "type") >>
        replaceText @"likely" "" >>
        replaceText @"unlikely" ""
        
    let private preprocess defines transforms (source, target) =
        Trace.logfn $"%s{source} -> %s{target}"
        [
            "//------------------------------------------------------------------------------\r\n//"
            "// This file has been generated. All changes will be lost."
            "//\r\n//------------------------------------------------------------------------------"
            for d in defines do $"#define %s{d}"
            ""
            source |> File.loadText
        ]
        |> String.concat "\r\n"
        |> transforms
        |> File.saveText target
        
    let convert64to32 root =
        !! (root @@ "**/x64/*64*.cs")
        |> Seq.map (fun fn -> fn, String.replace "64" "32" fn)
        |> Seq.iter (preprocess ["BIT32"] id)
       
    let convertAsyncToBlocking root =
        let strip =
            replaceText "async" "/*async*/" >>
            replaceText "await" "/*await*/" >>
            replaceExpr "Task[<](?<type>[A-Za-z0-9_]+[?]?)[>]" (fun g -> g "type") >>
            replaceText "Task" "void" >>
            replaceRaw "\\s*\\.Weave\\(\\)" "" >>
            id
    
        !! (root @@ "**/*.async.cs")
        |> Seq.map (fun fn -> fn, String.replace ".async." ".blocking." fn)
        |> Seq.iter (preprocess ["BLOCKING"] strip)
        
    let sanitizeOriginalSources () =
        let rules = basicTypes
        let sanitize fn =
            Trace.logfn $"Processing: %s{fn}"
            sanitize $"./orig/lib/%s{fn}" $"./src/sanitized/%s{fn}" rules
        sanitize "lz4.c"
        sanitize "lz4hc.c"
        sanitize "lz4.h"
        sanitize "lz4hc.h"
