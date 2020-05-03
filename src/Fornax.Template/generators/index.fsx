#r "../_lib/Fornax.Core.dll"
#load "layout.fsx"

open Html

let generate' (ctx : SiteContents) (_: string) =
  let posts = ctx.TryGetValues<Postloader.Post> () |> Option.defaultValue Seq.empty
  let siteInfo = ctx.TryGetValue<Globalloader.SiteInfo> ()
  let desc, postPageSize =
    siteInfo
    |> Option.map (fun si -> si.description, si.postPageSize)
    |> Option.defaultValue ("", 10)


  let psts =
    posts
    |> Seq.sortByDescending Layout.published
    |> Seq.toList
    |> List.chunkBySize postPageSize
    |> List.map (List.map (Layout.postLayout true))

  let pages = List.length psts

  let getFilenameForIndex i =
    if i = 0 then
      sprintf "index.html"
    else
      sprintf "posts/page%i.html" i

  let layoutForPostSet i psts =
    let nextPage =
      if i = (pages - 1) then "#"
      else "/" + getFilenameForIndex (i + 1)

    let previousPage =
      if i = 0 then "#"
      else "/" + getFilenameForIndex (i - 1)

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
      ]
      div [Class "container"] [
        div [Class "container has-text-centered"] [
          a [Href previousPage] [!! "Previous"]
          !! (sprintf "%i of %i" (i + 1) pages)
          a [Href nextPage] [!! "Next"]
        ]
      ]]

  psts
  |> List.mapi (fun i psts ->
    getFilenameForIndex i,
    layoutForPostSet i psts
    |> Layout.render ctx)

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    generate' ctx page