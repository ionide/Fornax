module Fornax

open System
open System.IO
open System.Threading
open Argu
open Suave
open Suave.Filters
open Suave.Operators

open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket

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


type [<CliPrefix(CliPrefix.DoubleDash)>] WatchOptions =
    | Port of int
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Port _ -> "Specify a custom port (default: 8080)"

type [<CliPrefix(CliPrefix.None)>] Arguments =
    | New
    | Build
    | Watch of ParseResults<WatchOptions>
    | Version
    | Clean
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | New -> "Create new web site"
            | Build -> "Build web site"
            | Watch _ -> "Start watch mode rebuilding "
            | Version -> "Print version"
            | Clean -> "Clean output and temp files"

/// Used to keep track of when content has changed,
/// thus triggering the websocket to update
/// any listeners to refresh.
let signalContentChanged = new Event<Choice<unit, Error>>()

let createFileWatcher dir handler =
    let fileSystemWatcher = new FileSystemWatcher()
    fileSystemWatcher.Path <- dir
    fileSystemWatcher.EnableRaisingEvents <- true
    fileSystemWatcher.IncludeSubdirectories <- true
    fileSystemWatcher.NotifyFilter <- NotifyFilters.DirectoryName ||| NotifyFilters.LastWrite ||| NotifyFilters.FileName
    fileSystemWatcher.Created.Add handler
    fileSystemWatcher.Changed.Add handler
    fileSystemWatcher.Deleted.Add handler

    /// Adding handler to trigger websocket/live refresh
    let contentChangedHandler _ =
        signalContentChanged.Trigger(Choice<unit,Error>.Choice1Of2 ())
    signalContentChanged.Trigger(Choice<unit,Error>.Choice1Of2 ())
    fileSystemWatcher.Created.Add contentChangedHandler
    fileSystemWatcher.Changed.Add contentChangedHandler
    fileSystemWatcher.Deleted.Add contentChangedHandler

    fileSystemWatcher

/// Websocket function that a page listens to so it
/// knows when to refresh.
let ws (webSocket : WebSocket) (context: HttpContext) =
    printfn "Opening WebSocket - new handShake"
    socket {
        try
            while true do
                do! Async.AwaitEvent signalContentChanged.Publish
                printfn "Signalling content changed"
                let emptyResponse = [||] |> ByteSegment
                do! webSocket.send Close emptyResponse true
        finally
            printfn "Disconnecting WebSocket"
    }

let getWebServerConfig port =
    match port with
    | Some port ->
        { defaultConfig with
            bindings =
                [ HttpBinding.create Protocol.HTTP Net.IPAddress.Loopback port ] }
    | None ->
        defaultConfig

let router basePath =
    choose [
        path "/" >=> Redirection.redirect "/index.html"
        (Files.browse (Path.Combine(basePath, "_public")))
        path "/websocket" >=> handShake ws
    ]

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "fornax",errorHandler=FornaxExiter())

    let results = parser.ParseCommandLine(inputs = argv).GetAllResults()

    if List.isEmpty results then
        printfn "No arguments provided.  Try 'fornax help' for additional details."
        printfn "%s" <| parser.PrintUsage()
        1
    elif List.length results > 1 then
        printfn "More than one command was provided.  Please provide only a single command.  Try 'fornax help' for additional details."
        printfn "%s" <| parser.PrintUsage()
        1
    else
        let result = List.tryHead results
        let cwd = Directory.GetCurrentDirectory ()

        match result with
        | Some New ->
            // The path of the directory that holds the scaffolding for a new website.
            let newTemplateDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blogTemplate")

            // The path of Fornax.Core.dll, which is located where the dotnet tool is installed.
            let corePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fornax.Core.dll")

            // Copy the folders from the template directory into the current folder.
            Directory.GetDirectories(newTemplateDir, "*", SearchOption.AllDirectories)
            |> Seq.iter (fun p -> Directory.CreateDirectory(p.Replace(newTemplateDir, cwd)) |> ignore)

            // Copy the files from the template directory into the current folder.
            Directory.GetFiles(newTemplateDir, "*.*", SearchOption.AllDirectories)
            |> Seq.iter (fun p -> File.Copy(p, p.Replace(newTemplateDir, cwd)))

            // Create the _bin directory in the current folder.  It holds
            // Fornax.Core.dll, which is used to provide Intellisense/autocomplete
            // in the .fsx files.
            Directory.CreateDirectory(Path.Combine(cwd, "_lib")) |> ignore

            // Copy the Fornax.Core.dll into _bin
            File.Copy(corePath, "./_lib/Fornax.Core.dll")

            printfn "New project successfully created."

            0
        | Some Build ->
            try
                do generateFolder cwd false
                0
            with
            | FornaxGeneratorException message ->
                Console.WriteLine message
                1
            | exn ->
                printfn "An unexpected error happend: %s%s%s" exn.Message Environment.NewLine exn.StackTrace
                1
        | Some (Watch watchOptions) ->
            let mutable lastAccessed = Map.empty<string, DateTime>
            let waitingForChangesMessage = "Generated site with errors. Waiting for changes..."

            let guardedGenerate () =
                try
                    do generateFolder cwd true
                with
                | FornaxGeneratorException message ->
                    printfn "%s%s%s" message Environment.NewLine waitingForChangesMessage
                | exn ->
                    printfn "An unexpected error happend: %O" exn
                    exit 1

            guardedGenerate ()

            use watcher = createFileWatcher cwd (fun e ->
                if not (e.FullPath.Contains "_public") && not (e.FullPath.Contains ".sass-cache") && not (e.FullPath.Contains ".git") && not (e.FullPath.Contains ".ionide") then
                    let lastTimeWrite = File.GetLastWriteTime(e.FullPath)
                    match lastAccessed.TryFind e.FullPath with
                    | Some lt when Math.Abs((lt - lastTimeWrite).Seconds) < 1 -> ()
                    | _ ->
                        printfn "[%s] Changes detected: %s" (DateTime.Now.ToString("HH:mm:ss")) e.FullPath
                        lastAccessed <- lastAccessed.Add(e.FullPath, lastTimeWrite)
                        guardedGenerate ())

            let webServerConfig = getWebServerConfig (watchOptions.TryPostProcessResult(<@ Port @>, uint16))
            startWebServerAsync webServerConfig (router cwd) |> snd |> Async.Start
            printfn "[%s] Watch mode started. Press any key to exit." (DateTime.Now.ToString("HH:mm:ss"))
            Console.ReadKey() |> ignore
            printfn "Exiting..."
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
