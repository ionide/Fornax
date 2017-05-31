#r "../_bin/Fornax.Core.dll"
#load "../siteModel.fsx"
#load "default.fsx"

open Html
open SiteModel

type Model = {
    title : string
    published : System.DateTime
}

let generate (siteModel : SiteModel) (mdl : Model) (posts : Post list) (content : string) =
    let post =
        article [Class "post"] [
            h1 [Class "post-title"] [!! mdl.title]
            div [Class "post-date"] [!! mdl.published.ToShortDateString() ]
            !! content
        ]

    Default.defaultPage siteModel mdl.title [post]