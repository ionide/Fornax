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
            article [ ClassName "post"] [
                h1 [ ClassName "post-title"] [
                    a [Href p.link] [ !! (defaultArg p.title "")]
                ]
                div [ClassName "post-date"] [(!! (defaultArg (p.published |> Option.map (fun p -> p.ToString())) ""))]
                !! content
            ]
        )

    Default.defaultPage siteModel mdl.title psts
