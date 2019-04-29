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
        replaceText @"char" "byte" >>
        replaceText @"BYTE" "byte" >>
        replaceText @"U16" "ushort" >>
        replaceText @"U32" "uint" >>
        replaceText @"U64" "ulong" >>
        replaceExpr @"const\s+(?<type>\w+)\s*\*\s+const" (fun g -> g "type" |> sprintf "%s*") >>
        replaceExpr @"const\s+(?<type>\w+)\s*\*" (fun g -> g "type" |> sprintf "%s*") >>
        replaceExpr @"(?<type>\w+)\s*\*\s+const" (fun g -> g "type" |> sprintf "%s*") >>
        replaceExpr @"(?<type>\w+)\s+const" (fun g -> g "type") >>
        replaceExpr @"const\s+(?<type>\w+)" (fun g -> g "type") >>
        replaceText @"likely" "" >>
        replaceText @"unlikely" ""
