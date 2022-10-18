[<AutoOpen>]
module Generator

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Text
open Config
open Logger

module EvaluatorHelpers =
    open FSharp.Quotations.Evaluator
    open FSharp.Reflection
    open System.Globalization
    open FSharp.Compiler.Interactive.Shell
    open System.Text

    let private sbOut = StringBuilder()
    let private sbErr = StringBuilder()
    let internal fsi (isWatch: bool) =
        let refs =
            ProjectSystem.FSIRefs.getRefs ()
            |> List.map (fun n -> sprintf "-r:%s" n)


        let inStream = new StringReader("")
        let outStream = new StringWriter(sbOut)
        let errStream = new StringWriter(sbErr)
        try
            let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
            let argv = [|
                yield! refs
                yield "--noframework"
                if isWatch then  yield "--define:WATCH"
                yield "--define:FORNAX"
                yield "/temp/fsi.exe"; |]
            FsiEvaluationSession.Create(fsiConfig, argv, inStream, outStream, errStream)
        with
        | ex ->
            errorfn "Error: %A" ex
            errorfn "Inner: %A" ex.InnerException
            errorfn "ErrorStream: %s" (errStream.ToString())
            raise ex


    let internal getOpen (path : string) =
        let filename = Path.GetFileNameWithoutExtension path
        let textInfo = (CultureInfo("en-US", false)).TextInfo
        textInfo.ToTitleCase filename

    let internal getLoad (path : string) =
        path.Replace("\\", "\\\\")

    let internal invokeFunction (f : obj) (args : obj seq) =
        // Recusive partial evaluation of f, terminate when no args are left.
        let rec helper (next : obj) (args : obj list)  =
            match args with
            | head::tail ->
                let fType = next.GetType()
                if FSharpType.IsFunction fType then
                    let methodInfo =
                        fType.GetMethods()
                        |> Array.filter (fun x -> x.Name = "Invoke" && x.GetParameters().Length = 1)
                        |> Array.head
                    let res = methodInfo.Invoke(next, [| head |])
                    helper res tail
                else None // Error case, arg exists but can't be applied
            | [] ->
                Some next
        helper f (args |> List.ofSeq )

    let internal compileExpression (input : FsiValue) =
        let genExpr = input.ReflectionValue :?> Quotations.Expr
        QuotationEvaluator.CompileUntyped genExpr

module LoaderEvaluator =
    open FSharp.Compiler.Interactive.Shell
    open EvaluatorHelpers

    let private runLoader (fsi : FsiEvaluationSession) (layoutPath : string) =
        let filename = getOpen layoutPath
        let load = getLoad layoutPath

        let tryFormatErrorMessage message (errors : 'a []) =
            if errors.Length > 0 then
                sprintf "%s: %A" message errors |> Some
            else
                None

        let _, loadErrors = fsi.EvalInteractionNonThrowing(sprintf "#load \"%s\";;" load)
        let loadErrorMessage =
            tryFormatErrorMessage "Load Errors" loadErrors

        let _, openErrors = fsi.EvalInteractionNonThrowing(sprintf "open %s;;" filename)
        let openErrorMessage =
            tryFormatErrorMessage "Open Errors" openErrors

        let funType, layoutErrors = fsi.EvalExpressionNonThrowing "<@@ fun a b -> loader a b @@>"
        let layoutErrorMessage =
            tryFormatErrorMessage "Get layout Errors" layoutErrors

        let completeErrorReport =
            [ loadErrorMessage
              openErrorMessage
              layoutErrorMessage ]
            |> List.filter (Option.isSome)
            |> List.map (fun v -> v.Value)
            |> List.fold (fun state message -> state + Environment.NewLine + message) ""
            |> (fun s -> s.Trim(Environment.NewLine.ToCharArray()))

        match funType with
        | Choice1Of2 (Some ft) ->
            Ok ft
        | _ -> Error completeErrorReport


    ///`loaderPath` - absolute path to `.fsx` file containing the loader
    let evaluate (fsi : FsiEvaluationSession) (siteContent : SiteContents) (loaderPath : string) (projectRoot : string)  =
        runLoader fsi loaderPath
        |> Result.bind (fun ft ->
            let generator = compileExpression ft
            invokeFunction generator [box projectRoot; box siteContent]
            |> function
                | Some r ->
                    try r :?> SiteContents |> Ok
                    with _ -> sprintf "File loader %s incorrect return type" loaderPath |> Error
                | None -> sprintf "File loader %s couldn't be compiled" loaderPath |> Error)

module GeneratorEvaluator =
    open FSharp.Compiler.Interactive.Shell
    open EvaluatorHelpers

    let private getGeneratorContent (fsi : FsiEvaluationSession) (generatorPath : string) =
        let filename = getOpen generatorPath
        let load = getLoad generatorPath

        let tryFormatErrorMessage message (errors : 'a []) =
            if errors.Length > 0 then
                sprintf "%s: %A" message errors |> Some
            else
                None

        let _, loadErrors = fsi.EvalInteractionNonThrowing(sprintf "#load \"%s\";;" load)
        let loadErrorMessage =
            tryFormatErrorMessage "Load Errors" loadErrors

        let _, openErrors = fsi.EvalInteractionNonThrowing(sprintf "open %s;;" filename)
        let openErrorMessage =
            tryFormatErrorMessage "Open Errors" openErrors

        let funType, layoutErrors = fsi.EvalExpressionNonThrowing "<@@ fun a b -> generate a b @@>"
        let layoutErrorMessage =
            tryFormatErrorMessage "Get generator Errors" layoutErrors

        let errors =
            [ loadErrorMessage
              openErrorMessage
              layoutErrorMessage ]
            |> List.choose id

        match errors, funType with
        | [], Choice1Of2 (Some ft) ->
            Ok (ft)
        | _, _ ->
            let completeErrorReport =
                errors
                |> List.fold (fun state message -> state + Environment.NewLine + message) ""
                |> fun s -> s.Trim(Environment.NewLine.ToCharArray())
            Error completeErrorReport

    let private generatorCache = new ConcurrentDictionary<_,_>()

    /// allows the cache to be cleared when running in watch mode and a change is detected
    let removeItemFromGeneratorCache() =
        generatorCache.Clear()

    ///`generatorPath` - absolute path to `.fsx` file containing the generator
    ///`projectRoot` - path to root of the site project
    ///`page` - path to the file that should be transformed
    let evaluate (fsi : FsiEvaluationSession) (siteContent : SiteContents) (generatorPath : string) (projectRoot: string) (page: string)  =

        let ok, generator = generatorCache.TryGetValue(generatorPath)
        let generator =
            if ok then
                generator
            else
                let generator =
                    getGeneratorContent fsi generatorPath
                    |> Result.bind (compileExpression >> Ok)
                generatorCache.AddOrUpdate(generatorPath, generator, fun key value -> value) |> ignore
                generator

        generator
        |> Result.bind (fun generator ->
            let result = invokeFunction generator [box siteContent; box projectRoot; box page ]

            result
            |> Option.bind (tryUnbox<string>)
            |> function
                | Some s -> Ok (Encoding.UTF8.GetBytes s)
                | None ->
                    result
                    |> Option.bind (tryUnbox<byte[]>)
                    |> function
                        | Some bytes -> Ok bytes
                        | None ->
                            sprintf "HTML generator %s couldn't be compiled" generatorPath |> Error)

    ///`generatorPath` - absolute path to `.fsx` file containing the generator
    ///`projectRoot` - path to root of the site project
    ///`page` - path to the file that should be transformed
    let evaluateMultiple (fsi : FsiEvaluationSession) (siteContent : SiteContents) (generatorPath : string) (projectRoot: string) (page: string)  =
        getGeneratorContent fsi generatorPath
        |> Result.bind (fun ft ->
            let generator = compileExpression ft

            let result = invokeFunction generator [box siteContent; box projectRoot; box page ]
            result
            |> Option.bind (tryUnbox<(string * string) list>)
            |> function
                | Some files -> Ok (files |> List.map (fun (o, r) -> o, Encoding.UTF8.GetBytes r))
                | None ->
                    result
                    |> Option.bind (tryUnbox<(string * byte[]) list>)
                    |> function
                        | Some s -> Ok s
                        | None -> sprintf "HTML generator %s couldn't be compiled" generatorPath |> Error)

module ConfigEvaluator =
    open FSharp.Compiler.Interactive.Shell
    open EvaluatorHelpers

    let private getConfig (fsi : FsiEvaluationSession) (configPath : string) =
        let filename = getOpen configPath
        let load = getLoad configPath

        let tryFormatErrorMessage message (errors : 'a []) =
            if errors.Length > 0 then
                sprintf "%s: %A" message errors |> Some
            else
                None

        let _, loadErrors = fsi.EvalInteractionNonThrowing(sprintf "#load \"%s\";;" load)
        let loadErrorMessage =
            tryFormatErrorMessage "Load Errors" loadErrors

        let _, openErrors = fsi.EvalInteractionNonThrowing(sprintf "open %s;;" filename)
        let openErrorMessage =
            tryFormatErrorMessage "Open Errors" openErrors

        let funType, layoutErrors = fsi.EvalExpressionNonThrowing "config"
        let layoutErrorMessage =
            tryFormatErrorMessage "Get generator Errors" layoutErrors

        let completeErrorReport =
            [ loadErrorMessage
              openErrorMessage
              layoutErrorMessage ]
            |> List.filter (Option.isSome)
            |> List.map (fun v -> v.Value)
            |> List.fold (fun state message -> state + Environment.NewLine + message) ""
            |> (fun s -> s.Trim(Environment.NewLine.ToCharArray()))

        match funType with
        | Choice1Of2 (Some ft) ->
            Ok (ft)
        | _ -> Error completeErrorReport


    ///`generatorPath` - absolute path to `.fsx` file containing the generator
    ///`projectRoot` - path to root of the site project
    ///`page` - path to the file that should be transformed
    let evaluate (fsi : FsiEvaluationSession) (siteContent : SiteContents) (generatorPath : string) =
        getConfig fsi generatorPath
        |> Result.bind (fun ft ->
            ft.ReflectionValue
            |> tryUnbox<Config.Config>
            |> function
                | Some s ->
                    siteContent.Add s
                    Ok s
                | None -> sprintf "Configuration evaluator %s couldn't be compiled" generatorPath |> Error)

exception FornaxGeneratorException of string

type GeneratorMessage = string

type GeneratorResult =
    | GeneratorIgnored
    | GeneratorSuccess of GeneratorMessage option
    | GeneratorFailure of GeneratorMessage

type GeneratorPick =
    | Simple of genartorPath : string * outputPath: string
    | Multiple of generatorPath: string * outputMapper: (string -> string)

let pickGenerator (cfg: Config.Config)  (siteContent : SiteContents) (projectRoot : string) (page: string) =
    let generator =
        match siteContent.TryGetError page with
        | Some _ -> None
        | None ->
            cfg.Generators |> List.tryFind (fun n ->
                match n.Trigger with
                | Once -> false //Once-trigger run globally, not for particular file
                | OnFile fn -> fn = page
                | OnFileExt ex -> ex = Path.GetExtension page
                | OnFilePredicate pred -> pred (projectRoot, page)
            )
    match generator with
    | None -> None
    | Some generator ->
        let generatorPath = Path.Combine(projectRoot, "generators", generator.Script)
        match generator.OutputFile with
        | MultipleFiles mapper ->
            Some(Multiple (generatorPath, mapper))
        | _ ->
            let outputPath =
                let newPage =
                    match generator.OutputFile with
                    | SameFileName -> page
                    | ChangeExtension(newExtension) -> Path.ChangeExtension(page, newExtension)
                    | NewFileName(newFileName) -> newFileName
                    | Custom(handler) -> handler page
                    | MultipleFiles(_) -> failwith "Shouldn't happen"
                Path.Combine(projectRoot, "_public", newPage)
            Some(Simple (generatorPath, outputPath))

///`projectRoot` - path to the root of website
///`page` - path to page that should be generated
let generate fsi (cfg: Config.Config) (siteContent : SiteContents) (projectRoot : string) (page : string) =
    let startTime = DateTime.Now
    match pickGenerator cfg siteContent projectRoot page with
    | None ->
        GeneratorIgnored
    | Some (Simple(layoutPath, outputPath)) ->

        let result = GeneratorEvaluator.evaluate fsi siteContent layoutPath projectRoot page
        match result with
        | Ok r ->
            let dir = Path.GetDirectoryName outputPath
            if not (Directory.Exists dir) then Directory.CreateDirectory dir |> ignore
            File.WriteAllBytes(outputPath, r)
            let endTime = DateTime.Now
            let ms = (endTime - startTime).Milliseconds
            sprintf "[%s] '%s' generated in %dms" (endTime.ToString("HH:mm:ss")) outputPath ms
            |> Some
            |> GeneratorSuccess
        | Error message ->
            let endTime = DateTime.Now
            sprintf "[%s] '%s' generation failed" (endTime.ToString("HH:mm:ss")) outputPath
            |> (fun s -> message + Environment.NewLine + s)
            |> GeneratorFailure
    | Some (Multiple(layoutPath, mapper)) ->
        let result = GeneratorEvaluator.evaluateMultiple fsi siteContent layoutPath projectRoot page
        match result with
        | Ok results ->
            results |>
            List.iter (fun (o, r) ->
                let outputPath = mapper o
                let outputPath = Path.Combine(projectRoot, "_public", outputPath)
                let dir = Path.GetDirectoryName outputPath
                if not (Directory.Exists dir) then Directory.CreateDirectory dir |> ignore
                File.WriteAllBytes(outputPath, r)
            )
            let endTime = DateTime.Now
            let ms = (endTime - startTime).Milliseconds
            sprintf "[%s] multiple files generated in %dms" (endTime.ToString("HH:mm:ss")) ms
            |> Some
            |> GeneratorSuccess
        | Error message ->
            let endTime = DateTime.Now
            sprintf "[%s] multiple files generation failed" (endTime.ToString("HH:mm:ss"))
            |> (fun s -> message + Environment.NewLine + s)
            |> GeneratorFailure

let runOnceGenerators fsi (cfg: Config.Config) (siteContent : SiteContents) (projectRoot : string) =
    cfg.Generators
    |> List.filter (fun n -> match n.Trigger with | Once -> true | _ -> false)
    |> List.filter (fun n -> match n.OutputFile with | NewFileName _ | MultipleFiles _ -> true | _ -> false)
    |> List.map (fun generator ->
        let startTime = DateTime.Now
        let generatorPath = Path.Combine(projectRoot, "generators", generator.Script)
        match generator.OutputFile with
        | NewFileName newFileName ->

            let outputPath = Path.Combine(projectRoot, "_public", newFileName)
            let result = GeneratorEvaluator.evaluate fsi siteContent generatorPath projectRoot ""
            match result with
            | Ok r ->
                let dir = Path.GetDirectoryName outputPath
                if not (Directory.Exists dir) then Directory.CreateDirectory dir |> ignore
                File.WriteAllBytes(outputPath, r)
                let endTime = DateTime.Now
                let ms = (endTime - startTime).Milliseconds
                sprintf "[%s] '%s' generated in %dms" (endTime.ToString("HH:mm:ss")) outputPath ms
                |> Some
                |> GeneratorSuccess
            | Error message ->
                let endTime = DateTime.Now
                sprintf "[%s] '%s' generation failed" (endTime.ToString("HH:mm:ss")) outputPath
                |> (fun s -> message + Environment.NewLine + s)
                |> GeneratorFailure
        | MultipleFiles mapper ->
            let result = GeneratorEvaluator.evaluateMultiple fsi siteContent generatorPath projectRoot ""
            match result with
            | Ok results ->
                results |>
                List.iter (fun (o, r) ->
                    let outputPath = mapper o
                    let outputPath = Path.Combine(projectRoot, "_public", outputPath)
                    let dir = Path.GetDirectoryName outputPath
                    if not (Directory.Exists dir) then Directory.CreateDirectory dir |> ignore
                    File.WriteAllBytes(outputPath, r)
                )
                let endTime = DateTime.Now
                let ms = (endTime - startTime).Milliseconds
                sprintf "[%s] multiple files generated in %dms" (endTime.ToString("HH:mm:ss")) ms
                |> Some
                |> GeneratorSuccess
            | Error message ->
                let endTime = DateTime.Now
                sprintf "[%s] multiple files generation failed" (endTime.ToString("HH:mm:ss"))
                |> (fun s -> message + Environment.NewLine + s)
                |> GeneratorFailure
        | _ -> failwith "Shouldn't happen"
    )


///`projectRoot` - path to the root of website
let generateFolder (sc : SiteContents) (projectRoot : string) (isWatch: bool) =
    let sw = Stopwatch.StartNew()

    let relative toPath fromPath =
        let toUri = Uri(toPath)
        let fromUri = Uri(fromPath)
        let relativeUri = toUri.MakeRelativeUri(fromUri).OriginalString
        Uri.UnescapeDataString(relativeUri)

    let projectRoot =
        if projectRoot.EndsWith (string Path.DirectorySeparatorChar) then projectRoot
        else projectRoot + (string Path.DirectorySeparatorChar)

    use fsi = EvaluatorHelpers.fsi (isWatch)
    let config =
        let configPath = Path.Combine(projectRoot, "config.fsx")
        if not (File.Exists configPath) then
            raise (FornaxGeneratorException "Couldn't find config.fsx")
        match ConfigEvaluator.evaluate fsi sc configPath with
        | Ok cfg -> cfg
        | Error error -> raise (FornaxGeneratorException error)

    let loaders = Directory.GetFiles(Path.Combine(projectRoot, "loaders"), "*.fsx")
    let sc =
        (sc, loaders)
        ||> Array.fold (fun state e ->
            match LoaderEvaluator.evaluate fsi state e projectRoot with
            | Ok sc ->
                sc
            | Error er ->
                errorfn "LOADER ERROR: %s" er
                state)
    sc.Errors() |> List.iter (fun er -> errorfn "BAD FILE: %s" er.Path)

    let logResult (result : GeneratorResult) =
        match result with
        | GeneratorIgnored -> ()
        | GeneratorSuccess None -> ()
        | GeneratorSuccess (Some message) ->
            okfn "%s" message
        | GeneratorFailure message ->
            // if one generator fails we want to exit early and report the problem to the operator
            raise (FornaxGeneratorException message)

    runOnceGenerators fsi config sc projectRoot
    |> List.iter logResult

    Directory.GetFiles(projectRoot, "*", SearchOption.AllDirectories)
    |> Array.iter (fun filePath ->
        filePath
        |> relative projectRoot
        |> generate fsi config sc projectRoot
        |> logResult)

    informationfn "Generation time: %A" sw.Elapsed
