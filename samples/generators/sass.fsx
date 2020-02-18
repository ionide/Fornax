#r "../../src/Fornax.Core/bin/Release/netstandard2.0/Fornax.Core.dll"

open System.IO
open System.Diagnostics

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    let inputPath = Path.Combine(projectRoot, page)
    let outputPath = Path.GetTempFileName ()

    let psi = ProcessStartInfo()
    psi.FileName <- "sass"
    psi.Arguments <- sprintf "%s %s" inputPath outputPath
    psi.CreateNoWindow <- true
    psi.WindowStyle <- ProcessWindowStyle.Hidden
    psi.UseShellExecute <- true
    try
        let proc = Process.Start psi
        proc.WaitForExit()
        let output = File.ReadAllText outputPath
        File.Delete outputPath
        output
    with
    | ex ->
        printfn "EX: %s" ex.Message
        printfn "Please check you have installed the Sass compiler if you are going to be using files with extension .scss. https://sass-lang.com/install"
        ""