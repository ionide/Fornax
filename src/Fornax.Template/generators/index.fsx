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

  let published (post: Postloader.Post) =
    post.published
    |> Option.defaultValue System.DateTime.Now
    |> fun n -> n.ToString("yyyy-MM-dd")


  let psts =
    posts
    |> Seq.sortByDescending published
    |> Seq.toList
    |> List.map (fun post ->
      div [Class "card article"] [
        div [Class "card-content"] [
          div [Class "media-content has-text-centered"] [
            p [Class "title article-title"; ] [ a [Href post.link] [!! post.title]]
            p [Class "subtitle is-6 article-subtitle"] [
              a [Href "#"] [!! (defaultArg post.author "")]
              !! (sprintf "on %s" (published post))
            ]
          ]
          div [Class "content article-body"] [
            !! post.content
          ]
        ]
      ])

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