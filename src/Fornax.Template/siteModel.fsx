#if DEBUG
#r "Facades/netstandard"
#endif
#I "./_bin/"

[<CLIMutable>]
type SiteModel = {
    title : string
    author_name: string
    url: string
}