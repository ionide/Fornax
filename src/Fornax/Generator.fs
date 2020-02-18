[<AutoOpen>]
module Generator

open System
open System.IO

module EvaluatorHelpers =
    open FSharp.Quotations.Evaluator
    open FSharp.Reflection
    open System.Globalization
    open FSharp.Compiler.Interactive.Shell
    open System.Text

    let private sbOut = StringBuilder()
    let private sbErr = StringBuilder()
    let internal fsi () =
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
                yield "/temp/fsi.exe";
                yield "--define:FORNAX"|]
            FsiEvaluationSession.Create(fsiConfig, argv, inStream, outStream, errStream)
        with
        | ex ->
            printfn "Error: %A" ex
            printfn "Inner: %A" ex.InnerException
            printfn "ErrorStream: %s" (errStream.ToString())
            raise ex


    let internal getOpen (path : string) =
        let filename = Path.GetFileNameWithoutExtension path
        let textInfo = (CultureInfo("en-US", false)).TextInfo
        textInfo.ToTitleCase filename

    let internal getLoad (path : string) =
        path.Replace("\\", "\\\\")

    let internal invokeFunction (f : obj) (args : obj seq) =
        let rec helper (next : obj) (args : obj list)  =
            match args.IsEmpty with
            | false ->
                let fType = next.GetType()
                if FSharpType.IsFunction fType then
                    let methodInfo =
                        fType.GetMethods()
                        |> Array.filter (fun x -> x.Name = "Invoke" && x.GetParameters().Length = 1)
                        |> Array.head
                    let res = methodInfo.Invoke(next, [| args.Head |])
                    helper res args.Tail
                else None
            | true ->
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
            let res = invokeFunction generator [box projectRoot; box siteContent]

            res
            |> Option.bind (tryUnbox<SiteContents>)
            |> function
                | Some s -> Ok s
                | None -> sprintf "The expression for %s couldn't be compiled" loaderPath |> Error)

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
    let evaluate (fsi : FsiEvaluationSession) (siteContent : SiteContents) (generatorPath : string) (projectRoot: string) (page: string)  =
        getGeneratorContent fsi generatorPath
        |> Result.bind (fun ft ->
            let generator = compileExpression ft

            invokeFunction generator [box siteContent; box projectRoot; box page ]
            |> Option.bind (tryUnbox<string>)
            |> function
                | Some s -> Ok s
                | None -> sprintf "The expression for %s couldn't be compiled" generatorPath |> Error)

exception FornaxGeneratorException of string

type GeneratorMessage = string

type GeneratorResult =
    | GeneratorIgnored
    | GeneratorSuccess of GeneratorMessage option
    | GeneratorFailure of GeneratorMessage

let pickGenerator (siteContent : SiteContents) (projectRoot : string) (page: string) =
    //TODO: THIS ALL SHOULD BE BASED ON SOME KIND OF CONFIGURATION
    let (|Ignored|Markdown|Less|Sass|StaticFile|) (filename : string) =
        let ext = Path.GetExtension filename
        if filename.Contains "_public" || filename.Contains "_bin" || filename.Contains "_lib" || filename.Contains "_data" || filename.Contains "_settings" || filename.Contains "_config.yml" || ext = ".fsx" || filename.Contains ".sass-cache" || filename.Contains ".git" || filename.Contains ".ionide" then Ignored
        elif ext = ".md" then Markdown
        elif ext = ".less" then Less
        elif ext = ".sass" || ext =".scss" then Sass
        else StaticFile
    match page with
    | Markdown ->
        let layoutPath = Path.Combine(projectRoot, "generators", "post.fsx")
        let outputPath =
            let p = Path.ChangeExtension(page, ".html")
            Path.Combine(projectRoot, "_public", p)
        Some(layoutPath, outputPath)
    | Less ->
        let layoutPath = Path.Combine(projectRoot, "generators", "less.fsx")
        let outputPath =
            let p = Path.ChangeExtension(page, ".css")
            Path.Combine(projectRoot, "_public", p)
        Some(layoutPath, outputPath)
    | Sass ->
        let layoutPath = Path.Combine(projectRoot, "generators", "sass.fsx")
        let outputPath =
            let p = Path.ChangeExtension(page, ".css")
            Path.Combine(projectRoot, "_public", p)
        Some(layoutPath, outputPath)
    | StaticFile ->
        let layoutPath = Path.Combine(projectRoot, "generators", "staticfile.fsx")
        let outputPath = Path.Combine(projectRoot, "_public", page)
        Some(layoutPath, outputPath)
    | Ignored ->
        None

///`projectRoot` - path to the root of website
///`page` - path to page that should be generated
let generate fsi (siteContent : SiteContents) (projectRoot : string) (page : string) =
    let startTime = DateTime.Now
    match pickGenerator siteContent projectRoot page with
    | None ->
        GeneratorIgnored
    | Some (layoutPath, outputPath) ->

    let result = GeneratorEvaluator.evaluate fsi siteContent layoutPath projectRoot page
    match result with
    | Ok r ->
        let dir = Path.GetDirectoryName outputPath
        if not (Directory.Exists dir) then Directory.CreateDirectory dir |> ignore
        File.WriteAllText(outputPath, r)
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


// Module to print colored message in the console
module Logger =
    let consoleColor (fc : ConsoleColor) =
        let current = Console.ForegroundColor
        Console.ForegroundColor <- fc
        { new IDisposable with
              member x.Dispose() = Console.ForegroundColor <- current }

    let informationfn str = Printf.kprintf (fun s -> use c = consoleColor ConsoleColor.Green in printfn "%s" s) str
    let error str = Printf.kprintf (fun s -> use c = consoleColor ConsoleColor.Red in printf "%s" s) str
    let errorfn str = Printf.kprintf (fun s -> use c = consoleColor ConsoleColor.Red in printfn "%s" s) str

///`projectRoot` - path to the root of website
let generateFolder  (projectRoot : string) =

    let relative toPath fromPath =
        let toUri = Uri(toPath)
        let fromUri = Uri(fromPath)
        toUri.MakeRelativeUri(fromUri).OriginalString

    let projectRoot =
        if projectRoot.EndsWith (string Path.DirectorySeparatorChar) then projectRoot
        else projectRoot + (string Path.DirectorySeparatorChar)

    use fsi = EvaluatorHelpers.fsi ()
    let loaders = Directory.GetFiles(Path.Combine(projectRoot, "loaders"), "*.fsx")
    let sc =
        (SiteContents (), loaders)
        ||> Array.fold (fun state e ->
            match LoaderEvaluator.evaluate fsi state e projectRoot with
            | Ok sc ->
                sc
            | Error er ->
                printfn "LOADER ERROR: %s" er
                state)


    let logResult (result : GeneratorResult) =
        match result with
        | GeneratorIgnored -> ()
        | GeneratorSuccess None -> ()
        | GeneratorSuccess (Some message) ->
            Logger.informationfn "%s" message
        | GeneratorFailure message ->
            // if one generator fails we want to exit early and report the problem to the operator
            raise (FornaxGeneratorException message)

    Directory.GetFiles(projectRoot, "*", SearchOption.AllDirectories)
    |> Array.iter (fun filePath ->
        filePath
        |> relative projectRoot
        |> generate fsi sc projectRoot
        |> logResult)

