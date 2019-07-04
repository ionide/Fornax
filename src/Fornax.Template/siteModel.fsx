#r "Facades/netstandard"
#r "../../Fornax.Core/bin/Debug/netstandard2.0/Fornax.Core.dll"

[<CLIMutable>]
type SiteModel = {
    title : string
    author_name: string
    url: string
}