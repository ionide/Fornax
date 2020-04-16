#r "../_lib/Fornax.Core.dll"
#load "layout.fsx"

open Html

let generate' (ctx : SiteContents) (_: string) =
  let posts = ctx.TryGetValues<Postloader.Post> () |> Option.defaultValue Seq.empty
  let siteInfo = ctx.TryGetValue<Globalloader.SiteInfo> ()
  let desc =
    siteInfo
    |> Option.map (fun si -> si.description)
    |> Option.defaultValue ""

  let psts =
    posts
    |> Seq.sortByDescending Layout.published
    |> Seq.toList
    |> List.map (Layout.postLayout true)

  Layout.layout ctx "Home" [
    section [Class "hero is-info is-medium is-bold"] [
      div [Class "hero-body"] [
        div [Class "container has-text-centered"] [
          h1 [Class "title"] [!!desc]
        ]
      ]
    ]
    div [Class "container"] [
      section [Class "articles"] [
        div [Class "column is-8 is-offset-2"] psts
      ]
    ]]

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
  generate' ctx page
  |> Layout.render ctx