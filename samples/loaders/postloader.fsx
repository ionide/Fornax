#r "../../src/Fornax.Core/bin/Release/netstandard2.0/Fornax.Core.dll"


type Test = {A: string}

let loader (projectRoot: string) (siteContet: SiteContents) =
    siteContet.Add({A = "asd"})
    siteContet.Add({A = projectRoot})
    siteContet