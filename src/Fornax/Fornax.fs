module Fornax

open System
open System.IO
open Argu

type [<CliPrefixAttribute("")>] Arguments =
    | New
    | Build
    | Watch
    | Version
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | New -> "Create new web site"
            | Build -> "Build web site"
            | Watch -> "Start watch mode rebuilding "
            | Version -> "Print version"

let toArguments (result : ParseResults<Arguments>) =
    if result.Contains <@ New @> then Some New
    elif result.Contains <@ Build @> then Some Build
    elif result.Contains <@ Watch @> then Some Watch
    elif result.Contains <@ Version @> then Some Version
    else None

let createFileWatcher dir handler =
    let fileSystemWatcher = new FileSystemWatcher()
    fileSystemWatcher.Path <- dir
    fileSystemWatcher.EnableRaisingEvents <- true
    fileSystemWatcher.IncludeSubdirectories <- true
    fileSystemWatcher.Created.Add handler
    fileSystemWatcher.Changed.Add handler
    fileSystemWatcher.Deleted.Add handler
    fileSystemWatcher.Renamed.Add handler
    fileSystemWatcher

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
        let cwd = Directory.GetCurrentDirectory ()
        match result with
        | Some New ->
            printfn "Not implemented"
            1
        | Some Build ->
            Generator.generateFolder cwd
            0
        | Some Watch ->
            use watcher = createFileWatcher cwd (fun e -> Generator.generateFolder cwd)
            printfn "Watch mode started. Press any key to exit"
            Generator.generateFolder cwd
            let _ = Console.ReadKey()
            0
        | Some Version ->
            printfn "%s" AssemblyVersionInformation.AssemblyVersion
            0
        | None ->
            printfn "Unknown argument"
            printfn "%s" <| parser.PrintUsage()
            1
