#r "../_bin/Fornax.Core.dll"
#load "../siteModel.fsx"
#load "default.fsx"

open Html
open SiteModel

type Model = {
    title : string
}

let generate (siteModel : SiteModel) (mdl : Model) (posts : Post list) (content : string) =
    let psts =
        posts
        |> List.map (fun p ->
            div [] [
                !! (defaultArg (p.published |> Option.map (fun p -> p.ToShortDateString())) "")
                !! "»"
                span [ Class "post-title"] [
                    a [Href p.link] [ !! p.title]
                ]
            ]
        )
    let ctn =
        div [] [
            yield h1 [] [!! "Archive"]
            yield! psts
        ]

    Default.defaultPage siteModel mdl.title [ctn]