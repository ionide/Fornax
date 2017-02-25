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
    ///`siteModel` - map cointaining input parameters for whole website
    ///`model` - map cointaining input parameters for given page
    ///`body` - content of the post (in html)
    let evaluate (templatePath : string) (siteModel : Map<string, obj>) (model : Map<string, obj>) (body : string) =
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
            let modelInput = createInstance mt model
            let siteInput = createInstance smt siteModel
            let generator = compileExpression ft
            invokeFunction generator [siteInput; modelInput; box body]
        | _ -> None

module ContentParser =
    open FsYaml

    let isSeparator (input : string) =
        input.StartsWith "---"

    ///`contentPath` - absolute path to `.md` file containing the page content
    let parse contentPath (modelType : Type) =
        let fileContent = File.ReadAllLines contentPath |> Array.skip 1 //First line must be ---
        let indexOfSeperator = fileContent |> Array.findIndex isSeparator
        let config, content = fileContent |> Array.splitAt indexOfSeperator
        let content = content |> Array.skip 1 |> String.concat "\n"
        let config = config |> String.concat "\n"
        let contentOutput = CommonMark.CommonMarkConverter.Convert content
        let configtOutput = Yaml.loadUntyped modelType config
        configtOutput, contentOutput

