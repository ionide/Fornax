module Fornax

open System
open System.IO

let private contentParser : string -> System.Type -> obj * string  = ParserUtils.memoizeParser Generator.ContentParser.parse
let private settingsParser : string -> System.Type -> obj = ParserUtils.memoizeParser Generator.SiteSettingsParser.parse
let private getLayout : string -> string = ParserUtils.memoize  Generator.ContentParser.getLayout

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