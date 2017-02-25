[<AutoOpen>]
module Generator

open System
open System.IO

module Evaluator =
    open System.Globalization
    open System.Text
    open Microsoft.FSharp.Compiler.Interactive.Shell
    open FSharp.Quotations.Evaluator
    open FSharp.Reflection


    let private sbOut = StringBuilder()
    let private sbErr = StringBuilder()

    let private fsi =
        let inStream = new StringReader("")
        let outStream = new StringWriter(sbOut)
        let errStream = new StringWriter(sbErr)
        try
            let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
            let argv = [| "/temo/fsi.exe"; |]
            FsiEvaluationSession.Create(fsiConfig, argv, inStream, outStream, errStream)
        with
        | ex ->
            printfn "Error: %A" ex
            printfn "Inner: %A" ex.InnerException
            printfn "ErrorStream: %s" (errStream.ToString())
            raise ex

    let private getOpen path =
        let filename = Path.GetFileNameWithoutExtension path
        let textInfo = (CultureInfo("en-US", false)).TextInfo
        textInfo.ToTitleCase filename

    let private getLoad (path : string) =
        path.Replace("\\", "\\\\")

    let private invokeFunction (f : obj) (args : obj seq) =
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

    let private createInstance (input : FsiValue) (args : Map<string, obj>) =
        let mType = input.ReflectionValue :?> Type
        let fields =
            mType.GetMembers()
            |> Array.skipWhile (fun n -> n.Name <> ".ctor")
            |> Array.skip 1
            |> Array.map (fun n -> args.[n.Name])

        let ctor = mType.GetConstructors().[0]
        ctor.Invoke(fields)

    let private compileExpression (input : FsiValue) =
        let genExpr = input.ReflectionValue :?> Quotations.Expr
        QuotationEvaluator.CompileUntyped genExpr

    ///`templatePath` - absolute path to `.fsx` file containing the template
    ///`getSiteModel` - function generating instance of site settings model of given type
    ///`getContentModel` - function generating instance of page mode of given type
    ///`body` - content of the post (in html)
    let evaluate (templatePath : string) (getSiteModel : System.Type -> obj) (getContentModel : System.Type -> obj * string) =
        let filename = getOpen templatePath
        let load = getLoad templatePath

        let _, errs = fsi.EvalInteractionNonThrowing(sprintf "#load \"%s\";;" load)
        if errs.Length > 0 then printfn "[ERROR 1] Load Erros : %A" errs

        let _, errs = fsi.EvalInteractionNonThrowing(sprintf "open %s;;" filename)
        if errs.Length > 0 then printfn "[ERROR 2] Open Erros : %A" errs

        let modelType, errs = fsi.EvalExpressionNonThrowing "typeof<Model>"
        if errs.Length > 0 then printfn "[ERROR 3] Get model Errors : %A" errs

        let siteModelType, errs = fsi.EvalExpressionNonThrowing "typeof<SiteModel.SiteModel>"
        if errs.Length > 0 then printfn "[ERROR 4] Get site model Errors : %A" errs

        let funType,errs = fsi.EvalExpressionNonThrowing "<@@ fun a b c -> (generate a b c) |> HtmlElement.ToString @@>"
        if errs.Length > 0 then printfn "[ERROR 5] Get template Errors : %A" errs

        match modelType, siteModelType, funType with
        | Choice1Of2 (Some mt), Choice1Of2 (Some smt), Choice1Of2 (Some ft) ->
            let modelInput, body = getContentModel (mt.ReflectionValue :?> Type)
            let siteInput = getSiteModel (smt.ReflectionValue :?> Type)
            let generator = compileExpression ft
            invokeFunction generator [siteInput; modelInput; box body]
            |> Option.bind (tryUnbox<string>)
        | _ -> None

module ContentParser =
    open FsYaml

    let private isSeparator (input : string) =
        input.StartsWith "---"

    let private isLayout (input : string) =
        input.StartsWith "layout:"

    ///`fileContent` - content of page to parse. Usually whole content of `.md` file
    ///`modelType` - `System.Type` representing type used as model of the page
    /// returns tupple of:
    /// - instance of model record
    /// - transformed to HTML page content
    let parse (fileContent : string) (modelType : Type) =
        let fileContent = fileContent.Split '\n'
        let fileContent = fileContent |> Array.skip 1 //First line must be ---
        let indexOfSeperator = fileContent |> Array.findIndex isSeparator
        let config, content = fileContent |> Array.splitAt indexOfSeperator

        let content = content |> Array.skip 1 |> String.concat "\n"
        let config = config |> String.concat "\n"
        let contentOutput = CommonMark.CommonMarkConverter.Convert content
        let configOutput = Yaml.loadUntyped modelType config
        configOutput, contentOutput

    ///`fileContent` - content of page to parse. Usually whole content of `.md` file
    ///returns name of template that should be used for the page
    let getLayout (fileContent : string) =
        fileContent.Split '\n'
        |> Array.find isLayout
        |> fun n -> n.Replace("layout:", "").Trim()

module SiteSettingsParser =
    open FsYaml

    ///`fileContent` - site settings to parse. Usually whole content of `site.yml` file
    ///`modelType` - `System.Type` representing type used as model of the global site settings
    let parse fileContent (modelType : Type) =
        Yaml.loadUntyped modelType fileContent

module internal ParserUtils =
    let memoizeParser f =
        let cache = ref Map.empty
        fun (x : string) (y : System.Type) ->
            let input = (x,y.GetHashCode())
            match (!cache).TryFind(input) with
            | Some res -> res
            | None ->
                let res = f x y
                cache := (!cache).Add(input,res)
                res

    let memoize f =
        let cache = ref Map.empty
        fun x ->
            match (!cache).TryFind(x) with
            | Some res -> res
            | None ->
                let res = f x
                cache := (!cache).Add(x,res)
                res


let private contentParser : string -> System.Type -> obj * string  = ParserUtils.memoizeParser ContentParser.parse
let private settingsParser : string -> System.Type -> obj = ParserUtils.memoizeParser SiteSettingsParser.parse
let private getLayout : string -> string = ParserUtils.memoize  ContentParser.getLayout

///`projectRoot` - path to the root of website
///`page` - path to page that should be generated
let generate (projectRoot : string) (page : string) =
    let contetPath = Path.Combine(projectRoot, page)
    let settingsPath = Path.Combine(projectRoot, "site.yaml")
    let outputPath =
        let p = Path.ChangeExtension(page, ".html")
        Path.Combine(projectRoot, "_site", p)

    let contentText = File.ReadAllText contetPath
    let settingsText = File.ReadAllText settingsPath
    let layout = getLayout contentText

    let settingsLoader = settingsParser settingsText
    let modelLoader = contentParser contentText
    let templatePath = Path.Combine(projectRoot, "templates", layout + ".fsx")

    let startTime = DateTime.Now
    let result = Evaluator.evaluate templatePath settingsLoader modelLoader

    match result with
    | Some r ->
        let dir = Path.GetDirectoryName outputPath
        if not (Directory.Exists dir) then Directory.CreateDirectory dir |> ignore
        File.WriteAllText(outputPath, r)
        let endTime = DateTime.Now
        let ms = (endTime - startTime).Milliseconds
        printfn "[%s] '%s' generated in %dms" (endTime.ToString("HH:mm:ss")) outputPath ms
    | None ->
        let endTime = DateTime.Now
        printfn "[%s] '%s' generation failed" (endTime.ToString("HH:mm:ss")) outputPath

///`projectRoot` - path to the root of website
let generateFolder (projectRoot : string) =
    Directory.GetFiles(projectRoot, "*.md", SearchOption.AllDirectories)
    |> Array.iter (generate projectRoot)