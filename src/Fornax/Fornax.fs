module Fornax

open Argu

type Arguments =
    | New
    | Build
    | Watch
    | Version
    | Help
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | New -> "Create new web site"
            | Build -> "Build web site"
            | Watch -> "Start watch mode rebuilding "
            | Version -> "Print version"
            | Help -> "Print help"

let toArguments (result : ParseResults<Arguments>) =
    if result.Contains <@ New @> then New
    elif result.Contains <@ Build @> then Build
    elif result.Contains <@ Watch @> then Watch
    elif result.Contains <@ Version @> then Version
    else Help



[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "fornax")

    if argv.Length = 0 then
        printfn "No arguments provided."
        printfn "%s" <| parser.PrintUsage()
        1
    elif argv.Length > 1 then
        printfn "Provide only 1 argument"
        printfn "%s" <| parser.PrintUsage()
        1
    else
        let result = parser.Parse argv |> toArguments
        printfn "%A" argv
        0 // return an integer exit code
