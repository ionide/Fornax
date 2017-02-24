#load "../paket-files/include-scripts/net46/include.main.group.fsx"
#r "../build/Fornax.dll"
let ctn = [("Name", unbox<obj> "Ja"); ("Surname", unbox "Ja2")] |> Map.ofSeq
let siteModel =  [("SomeGlobalValue", unbox<obj> "ValueFromSiteModel")] |> Map.ofSeq
let path = System.IO.Path.GetFullPath "samples/sample.fsx"
let res = Generator.Evaluator.evaluate path siteModel ctn "ttt"
res


// --------------------------------

let contentPath = System.IO.Path.GetFullPath "samples/post.md"
Generator.ContentParser.parse contentPath


