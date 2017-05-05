#r "../../temp/build/Fornax.Core.dll"
#load "../siteModel.fsx"

open Html
open SiteModel

type Model = {
    Name : string
    Surname : string
}

let generate  (siteModel : SiteModel) (mdl : Model) (posts : Post list) (content : string) =
    let psts = posts |> List.map (fun p -> span [] [!! p.link] )

    html [] [
        div [] [
            span [] [ !! ("Hello world " + mdl.Name) ]
            span [] [ !! content ]
            span [] [ !! siteModel.SomeGlobalValue ]
        ]
        div [] psts
    ]
