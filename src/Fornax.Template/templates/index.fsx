// #r "../_bin/Fornax.Core.dll"
// #r "../../Fornax.Core/bin/Debug/netstandard2.0/Fornax.Core.dll"
#load "../siteModel.fsx"
#load "default.fsx"

open Html
open SiteModel


[<CLIMutable>]
type Model = {
    title : string
}

let generate (siteModel : SiteModel) (mdl : Model) (posts : Post list) (content : string) =
    let psts =
        posts
        |> List.map (fun p ->
            article [ Class "post"] [
                h1 [ Class "post-title"] [
                    a [Href p.link] [ !! p.title ]
                ]
                div [Class "post-date"] [(!! (defaultArg (p.published |> Option.map (fun p -> p.ToShortDateString())) ""))]
                !! p.content
            ]
        )

    Default.defaultPage siteModel mdl.title psts
