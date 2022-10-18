module Fornax

open System
open System.IO
open System.Threading
open Argu
open Suave
open Suave.Filters
open Suave.Operators

open LibGit2Sharp
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open System.Reflection
open Logger

type FornaxExiter () =
    interface IExiter with
        member x.Name = "fornax exiter"
        member x.Exit (msg, errorCode) =
            if errorCode = ErrorCode.HelpText then
                printf "%s" msg
                exit 0
            else
                errorfn "Error with code %A received - exiting." errorCode
                printf "%s" msg
                exit 1


type [<CliPrefix(CliPrefix.DoubleDash)>] WatchOptions =
    | Port of int
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Port _ -> "Specify a custom port (default: 8080)"

type [<CliPrefix(CliPrefix.DoubleDash)>] NewOptions =
    | [<AltCommandLine("-t")>] Template of string
    | [<AltCommandLine("-o")>] Output of string
with
    interface IArgParserTemplate with
        member s.Usage = 
            match s with
            | Template _ -> "Specify a template from an HTTPS git repo or local folder"
            | Output _ -> "Specify an output folder"

type [<CliPrefix(CliPrefix.None)>] Arguments =
    | New of ParseResults<NewOptions>
    | Build
    | Watch of ParseResults<WatchOptions>
    | Version
    | Clean
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | New _ -> "Create new web site"
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
        GeneratorEvaluator.removeItemFromGeneratorCache()

    signalContentChanged.Trigger(Choice<unit,Error>.Choice1Of2 ())
    fileSystemWatcher.Created.Add contentChangedHandler
    fileSystemWatcher.Changed.Add contentChangedHandler
    fileSystemWatcher.Deleted.Add contentChangedHandler

    fileSystemWatcher

/// Websocket function that a page listens to so it
/// knows when to refresh.
let ws (webSocket : WebSocket) (context: HttpContext) =
    informationfn "Opening WebSocket - new handShake"
    socket {
        try
            while true do
                do! Async.AwaitEvent signalContentChanged.Publish
                informationfn "Signalling content changed"
                let emptyResponse = [||] |> ByteSegment
                do! webSocket.send Close emptyResponse true
        finally
            informationfn "Disconnecting WebSocket"
    }

let getWebServerConfig port =
    match port with
    | Some port ->
        { defaultConfig with
            bindings =
                [ HttpBinding.create Protocol.HTTP Net.IPAddress.Loopback port ] }
    | None ->
        defaultConfig

let getOutputDirectory (output : option<string>) (cwd : string) = 
    match output with
    | Some output ->
        output
    | None ->
        cwd

// Recursively unset read-only attributes inside a folder 
// Like, say, .git
let normalizeFiles directory =
    Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
    |> Seq.iter (fun path -> File.SetAttributes(path, FileAttributes.Normal))

    directory

let deleteDirectory directory =
    Directory.Delete(directory, true)

let deleteGit (gitDirectory : string) =
    let test = Directory.Exists gitDirectory

    match test with
    | true -> gitDirectory |> normalizeFiles |> deleteDirectory  
    | false -> ()
        
let copyDirectories (input : string) (output : string) = 
    // Copy the folders from the template directory into the current folder.
    Directory.GetDirectories(input, "*", SearchOption.AllDirectories)
    |> Seq.iter (fun p -> Directory.CreateDirectory(p.Replace(input, output)) |> ignore)

    // Copy the files from the template directory into the current folder.
    Directory.GetFiles(input, "*.*", SearchOption.AllDirectories)
    |> Seq.iter (fun p -> File.Copy(p, p.Replace(input, output)))

let handleTemplate (template : option<string>) (outputDirectory : string) : unit = 
    match template with
    | Some template ->
        let uriTest, _ = Uri.TryCreate(template, UriKind.Absolute)

        match uriTest with
        | true  -> Repository.Clone(template, outputDirectory) |> ignore
                   Path.Combine(outputDirectory, ".git") |> deleteGit
        | false -> copyDirectories template outputDirectory
                   Path.Combine(outputDirectory, ".git") |> deleteGit
    | None ->
        // The default path of the directory that holds the scaffolding for a new website.
        let path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blogTemplate")
        copyDirectories path outputDirectory

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
        errorfn "No arguments provided.  Try 'fornax help' for additional details."
        printfn "%s" <| parser.PrintUsage()
        1
    elif List.length results > 1 then
        errorfn "More than one command was provided.  Please provide only a single command.  Try 'fornax help' for additional details."
        printfn "%s" <| parser.PrintUsage()
        1
    else
        let result = List.tryHead results
        let cwd = Directory.GetCurrentDirectory ()

        match result with
        | Some (New newOptions) ->
            // The path of Fornax.Core.dll, which is located where the dotnet tool is installed.
            let corePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fornax.Core.dll")

            // Parse output flag and create a folder in the cwd to copy files to
            let outputDirectory = getOutputDirectory (newOptions.TryPostProcessResult(<@ Output @>, string)) cwd

            // Handle the template used to scaffold a new website
            handleTemplate (newOptions.TryPostProcessResult(<@ Template @>, string)) (outputDirectory)

            // Create the _lib directory in the current folder.  It holds
            // Fornax.Core.dll, which is used to provide Intellisense/autocomplete
            // in the .fsx files.
            Path.Combine(outputDirectory, "_lib")
            |> Directory.CreateDirectory
            |> ignore

            // Copy the Fornax.Core.dll into _lib
            // Some/most times Fornax.Core.dll already exists
            File.Copy(corePath, outputDirectory + "/_lib/Fornax.Core.dll", true)
            okfn "New project successfully created."
            0
        | Some Build ->
            try
                let sc = SiteContents ()
                do generateFolder sc cwd false
                0
            with
            | FornaxGeneratorException message ->
                message |> stringFormatter |> errorfn
                1
            | exn ->
                errorfn "An unexpected error happend: %O" exn
                1
        | Some (Watch watchOptions) ->
            let mutable lastAccessed = Map.empty<string, DateTime>
            let waitingForChangesMessage = "Generated site with errors. Waiting for changes..."

            let sc = SiteContents ()


            let guardedGenerate () =
                try
                    do generateFolder sc cwd true
                with
                | FornaxGeneratorException message ->
                    message |> stringFormatter |> errorfn 
                    waitingForChangesMessage |> stringFormatter |> informationfn
                | exn ->
                    errorfn "An unexpected error happend: %O" exn
                    exit 1

            guardedGenerate ()

            use watcher = createFileWatcher cwd (fun e ->
                let pathDirectories = 
                    Path.GetRelativePath(cwd,e.FullPath)
                        .Split(Path.DirectorySeparatorChar)
                
                let shouldHandle =
                    pathDirectories
                    |> Array.exists (fun fragment ->
                        fragment = "_public" ||     
                        fragment = ".sass-cache" ||    
                        fragment = ".git" ||           
                        fragment = ".ionide")
                    |> not

                if shouldHandle then
                    let lastTimeWrite = File.GetLastWriteTime(e.FullPath)
                    match lastAccessed.TryFind e.FullPath with
                    | Some lt when Math.Abs((lt - lastTimeWrite).Seconds) < 1 -> ()
                    | _ ->
                        informationfn "[%s] Changes detected: %s" (DateTime.Now.ToString("HH:mm:ss")) e.FullPath
                        lastAccessed <- lastAccessed.Add(e.FullPath, lastTimeWrite)
                        guardedGenerate ())

            let webServerConfig = getWebServerConfig (watchOptions.TryPostProcessResult(<@ Port @>, uint16))
            startWebServerAsync webServerConfig (router cwd) |> snd |> Async.Start
            okfn "[%s] Watch mode started." (DateTime.Now.ToString("HH:mm:ss"))
            informationfn "Press any key to exit."
            Console.ReadKey() |> ignore
            informationfn "Exiting..."
            0
        | Some Version -> 
            let assy = Assembly.GetExecutingAssembly()
            let v = assy.GetCustomAttributes<AssemblyVersionAttribute>() |> Seq.head
            printfn "%s" v.Version
            0
        | Some Clean ->
            let publ = Path.Combine(cwd, "_public")
            let sassCache = Path.Combine(cwd, ".sass-cache")
            let deleter folder = 
                match Directory.Exists(folder) with
                | true -> Directory.Delete(folder, true)
                | _ -> () 
            try
                [publ ; sassCache] |> List.iter deleter
                0
            with
            | _ -> 1
        | None ->
            errorfn "Unknown argument"
            printfn "%s" <| parser.PrintUsage()
            1
