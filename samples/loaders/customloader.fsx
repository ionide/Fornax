#r "../../src/Fornax.Core/bin/Release/netstandard2.0/Fornax.Core.dll"

type CustomConfig = {
    SomeGlobalValue: string
}


let loader (projectRoot: string) (siteContet: SiteContents) =
    siteContet.Add({SomeGlobalValue = "some global per site value"})
    siteContet