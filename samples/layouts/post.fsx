#if !FORNAX
#r "../../src/Fornax.Core/bin/Release/netstandard2.0/Fornax.Core.dll"
#load "../loaders/postloader.fsx"
#endif

open Html

[<CLIMutable>]
type Model = {
    Name : string
    Surname : string
}


let generate (ctx : SiteContents) (mdl : Model)  (content : string) =
    let tests = ctx.TryGetValues<Postloader.Test> ()
    printfn "TESTS: %A" tests

    let posts = ctx.TryGetValues<Post> ()
    let psts =
        match posts with
        | Some posts ->
            posts
            |> Seq.toList
            |> List.map (fun p -> span [] [!! p.link] )
        | None -> []

    // let siteModel = ctx.TryGetValue<SiteModel> ()
    // let gv = siteModel |> Option.map (fun s -> s.SomeGlobalValue) |> Option.defaultValue "NO DEAFULT"

    html [] [
        div [] [
            span [] [ !! ("Hello world " + mdl.Name) ]
            span [] [ !! content ]
            // span [] [ !! gv ]
        ]
        div [] psts
    ]
