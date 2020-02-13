#load "build.imports.fsx"

namespace Tools

open System.IO
open System.Text.RegularExpressions

module Sanitizer =
    let private b = @"(\b|\s|^|$|[\(\)\[\]])"
    let sanitize source target rules =
        File.WriteAllText (target, rules (File.ReadAllText source))
    let replaceText pattern (value: string) content =
        Regex.Replace(content, sprintf @"(?<=%s)(%s)(?=%s)" b pattern b, value)
    let replaceExpr pattern evaluator content =
        let evaluator = MatchEvaluator(fun m -> evaluator (fun (n: string) -> m.Groups.[n].Value))
        Regex.Replace(content, sprintf @"(?<=%s)(%s)(?=%s)" b pattern b, evaluator)
    let rec repeat replacer content =
        match replacer content with
        | c when c = content -> content
        | c -> repeat replacer c

    let basicTypes =
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
        replaceText @"uptrval" "ptr_t" >>
        replaceText @"base" "@base" >>
        replaceText @"LZ4_writeLE16" "Mem.Poke2" >>
        replaceText @"LZ4_read16" "Mem.Peek2" >>
        replaceText @"LZ4_read32" "Mem.Peek4" >>
        replaceText @"LZ4_write32" "Mem.Poke4" >>
        replaceText @"LZ4_read_ARCH" "Mem.PeekW" >>
        replaceText @"LZ4_wildCopy8" "Mem.WildCopy8" >>
        replaceText @"memcpy" "Mem.Copy" >>
        replaceText @"memmove" "Mem.Move" >>
        replaceText @"assert" "Debug.Assert" >>
        replaceExpr @"const\s+(?<type>\w+)\s*\*\s+const" (fun g -> g "type" |> sprintf "%s*") >>
        replaceExpr @"const\s+(?<type>\w+)\s*\*" (fun g -> g "type" |> sprintf "%s*") >>
        replaceExpr @"(?<type>\w+)\s*\*\s+const" (fun g -> g "type" |> sprintf "%s*") >>
        replaceExpr @"(?<type>\w+)\s+const" (fun g -> g "type") >>
        replaceExpr @"const\s+(?<type>\w+)" (fun g -> g "type") >>
        replaceText @"likely" "" >>
        replaceText @"unlikely" ""
