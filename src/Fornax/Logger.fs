module Logger

open System

let consoleColor (fc : ConsoleColor) =
    let current = Console.ForegroundColor
    Console.ForegroundColor <- fc
    { new IDisposable with
            member x.Dispose() = Console.ForegroundColor <- current }

let stringFormatter str = Printf.StringFormat<_, unit>(str) 
let informationfn str = Printf.kprintf (fun s -> use c = consoleColor ConsoleColor.Cyan in printfn "%s" s) str
let errorfn str = Printf.kprintf (fun s -> use c = consoleColor ConsoleColor.Red in printfn "%s" s) str
let okfn str = Printf.kprintf (fun s -> use c = consoleColor ConsoleColor.Green in printfn "%s" s) str