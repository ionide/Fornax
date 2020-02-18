#r "../../src/Fornax.Core/bin/Release/netstandard2.0/Fornax.Core.dll"
#r "../_lib/dotless.Core.dll"

open System.IO

let parseLess fileContent =
    dotless.Core.Less.Parse fileContent

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    let inputPath = Path.Combine(projectRoot, page)
    File.ReadAllText inputPath |> parseLess