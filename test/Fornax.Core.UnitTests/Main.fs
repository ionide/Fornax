module Fornax.Core.UnitTests

open Expecto

[<EntryPoint>]
let main argv =
    Tests.runTestsInAssemblyWithCLIArgs [||] argv
