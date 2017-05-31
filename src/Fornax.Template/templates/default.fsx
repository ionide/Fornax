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
            div [Class "container"] [
                header [ Class "masthead"] [
                    h3 [Class "masthead-title"] [
                        a [ Href "/" ] [ !! (siteModel.title)]
                        small [ Class "masthead-link" ] [
                            a [ Href "/archive.html"] [ !! "Archive"]
                        ]
                    ]
                ]

                div [ Id "content"] content
            ]
        ]
    ]