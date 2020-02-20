#r "../_lib/Fornax.Core.dll"

type Page = {
    title: string
    link: string
}

let loader (projectRoot: string) (siteContet: SiteContents) =
    siteContet.Add({title = "Home"; link = "/"})
    siteContet.Add({title = "About"; link = "/about.html"})
    siteContet.Add({title = "Contact"; link = "/contact.html"})

    siteContet