module Generator

open System
open System.IO

module Evaluator =
    open System.Globalization
    open System.Text
    open Microsoft.FSharp.Compiler.Interactive.Shell
    open FSharp.Quotations.Evaluator


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

    ///`templatePath` - absolute path to `.fsx` file containing the template
    ///`content` - map cointaining input parameters for template
    let evaluate (templatePath : string) (content : Map<string, obj>) =
        let filename = getOpen templatePath
        let load = getLoad templatePath

        let _, errs = fsi.EvalInteractionNonThrowing(sprintf "#load \"%s\";;" load)
        if errs.Length > 0 then printfn "[ERROR 1] Load Erros : %A" errs

        let _, errs = fsi.EvalInteractionNonThrowing(sprintf "open %s;;" filename)
        if errs.Length > 0 then printfn "[ERROR 2] Open Erros : %A" errs

        let modelType, errs = fsi.EvalExpressionNonThrowing "typeof<Model>"
        if errs.Length > 0 then printfn "[ERROR 3] Get model Errors : %A" errs

        let res,errs = fsi.EvalExpressionNonThrowing "<@@ generate >> HtmlElement.ToString @@>"
        if errs.Length > 0 then printfn "[ERROR 4] Get template Errors : %A" errs

        match modelType, res with
        | Choice1Of2 (Some t), Choice1Of2 (Some f) ->
            let mType = t.ReflectionValue :?> Type
            let fields =
                mType.GetMembers()
                |> Array.skipWhile (fun n -> n.Name <> ".ctor")
                |> Array.skip 1
                |> Array.map (fun n -> content.[n.Name])

            let ctor = mType.GetConstructors().[0]
            let input = ctor.Invoke(fields)

            let genExpr = f.ReflectionValue :?> Quotations.Expr
            let genMethod = QuotationEvaluator.CompileUntyped genExpr
            let output = genMethod.GetType().InvokeMember("Invoke",System.Reflection.BindingFlags.InvokeMethod,null,genMethod,[| box input |]) |> unbox<string>

            Some output
        | _ -> None



