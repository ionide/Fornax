#r "../_bin/Fornax.Core.dll"
#load "../siteModel.fsx"


open Html
open SiteModel

let defaultPage (siteModel : SiteModel) pageTitle content =
    html [] [
        head [] [
            meta [CharSet "utf-8"] []
            title [] [ (!! pageTitle) ]
            link [ Rel "stylesheet"; Type "text/css"; Href "/css/style.css" ] []
            link [ Rel "alternate"; Type "application/atom+xml"; Href "/feed.xml"; HtmlProperties.Title "News Feed" ] []

        ]
        body [] [
            div [ClassName "container"] [
                header [ ClassName "masthead"] [
                    h3 [ClassName "masthead-title"] [
                        a [ Href "/" ] [ !! (siteModel.title)]
                        small [ Href "/archive.html"] [ !! "Archive"]
                    ]
                ]

                div [ Id "content"] content
            ]
        ]
    ]