#r "../../build/Fornax.Core.dll"
#load "../siteModel.fsx"

open Html
open SiteModel

type Model = {
    Name : string
    Surname : string
}

let generate (siteModel : SiteModel) (mdl : Model) (content : string) =
    html [] [
        div [] [
            span [] [ !! ("Hello world " + mdl.Name) ]
            span [] [ !! content ]
            span [] [ !! siteModel.SomeGlobalValue ]
        ]
    ]
