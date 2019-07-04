module Fornax

open System
open System.IO
open Argu
open Suave
open Suave.Filters
open Suave.Operators

type FornaxExiter () =
    interface IExiter with
        member x.Name = "fornax exiter"
        member x.Exit (msg, errorCode) =
            if errorCode = ErrorCode.HelpText then
                printfn "%s" msg
                exit 0
            else
                printfn "Error with code %A received - exiting." errorCode
                printfn "%s" msg
                exit 1

type [<CliPrefixAttribute("")>] Arguments =
    | New
    | Build
    | Watch
    | Version
    | Clean
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | New -> "Create new web site"
            | Build -> "Build web site"
            | Watch -> "Start watch mode rebuilding "
            | Version -> "Print version"
            | Clean -> "Clean output and temp files"

let toArguments (result : ParseResults<Arguments>) =
    if result.Contains <@ New @> then Some New
    elif result.Contains <@ Build @> then Some Build
    elif result.Contains <@ Watch @> then Some Watch
    elif result.Contains <@ Version @> then Some Version
    elif result.Contains <@ Clean @> then Some Clean

    else None

let createFileWatcher dir handler =
    let fileSystemWatcher = new FileSystemWatcher()
    fileSystemWatcher.Path <- dir
    fileSystemWatcher.EnableRaisingEvents <- true
    fileSystemWatcher.IncludeSubdirectories <- true
    fileSystemWatcher.NotifyFilter <- NotifyFilters.DirectoryName ||| NotifyFilters.LastWrite ||| NotifyFilters.FileName
    fileSystemWatcher.Created.Add handler
    fileSystemWatcher.Changed.Add handler
    fileSystemWatcher.Deleted.Add handler
    fileSystemWatcher

let router basePath =
    choose [
        path "/" >=> Redirection.redirect "/index.html"
        (Files.browse (Path.Combine(basePath, "_public")))
    ]

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "fornax", errorHandler = FornaxExiter ())

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
            let templateDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "newTemplate")
            let corePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fornax.Core.dll")

            Directory.GetDirectories(templateDir, "*", SearchOption.AllDirectories)
            |> Seq.iter (fun p -> Directory.CreateDirectory(p.Replace(templateDir, cwd)) |> ignore)

            Directory.GetFiles(templateDir, "*.*", SearchOption.AllDirectories)
            |> Seq.iter (fun p -> File.Copy(p, p.Replace(templateDir, cwd)))

            Directory.CreateDirectory(Path.Combine(cwd, "_bin")) |> ignore
            
            File.Copy(corePath, "./_bin/Fornax.Core.dll")

            0
        | Some Build ->
            Generator.generateFolder cwd
            0
        | Some Watch ->
            let mutable lastAccessed = Map.empty<string, DateTime>
            printfn "[%s] Watch mode started. Press any key to exit" (DateTime.Now.ToString("HH:mm:ss"))
            startWebServerAsync defaultConfig (router cwd) |> snd |> Async.Start
            Generator.generateFolder cwd
            use watcher = createFileWatcher cwd (fun e ->
                if not (e.FullPath.Contains "_public") && not (e.FullPath.Contains ".sass-cache") && not (e.FullPath.Contains ".git") then
                    let lastTimeWrite = File.GetLastWriteTime(e.FullPath)
                    match lastAccessed.TryFind e.FullPath with
                    | Some lt when Math.Abs((lt - lastTimeWrite).Seconds) < 1 -> ()
                    | _ ->
                        printfn "[%s] Changes detected: %s" (DateTime.Now.ToString("HH:mm:ss")) e.FullPath
                        lastAccessed <- lastAccessed.Add(e.FullPath, lastTimeWrite)
                        Generator.generateFolder cwd)
            let _ = Console.ReadKey()
            0
        | Some Version ->
            printfn "%s" AssemblyVersionInformation.AssemblyVersion
            0
        | Some Clean ->
            let publ = Path.Combine(cwd, "_public")
            let sassCache = Path.Combine(cwd, ".sass-cache")
            try
                Directory.Delete(publ, true)
                Directory.Delete(sassCache, true)
                0
            with
            | _ -> 1
        | None ->
            printfn "Unknown argument"
            printfn "%s" <| parser.PrintUsage()
            1