#r "../../src/Fornax.Core/bin/Debug/netstandard2.0/Fornax.Core.dll"
#load "../siteModel.fsx"

open Html
open SiteModel

[<CLIMutable>]
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
