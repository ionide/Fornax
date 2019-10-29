module Forax.UnitTests.FornaxConfigurationTests

open Expecto
open Configuration.FornaxConfiguration

let emptyconfig = ""

let configOnlyExcludeTwoItems =
    """exclude:
- "node_modules/"
- "packages/"
    """

let configExcludesAndStyleEntry =
    """exclude:
- "node_modules/"
- "packages/"

style:
  entry: "main.scss"
"""

[<Tests>]
let fornaxConfigTests =
    testList "Test Fornax Config Generation" [
        testCase "That empty config is parsed with defaults." <| fun _ ->
            let config = parseFornaxConfiguration emptyconfig
            Expect.equal 0 config.Exclude.Length "An empty config should hold contain no excludes."

        testCase "That exclude block is parsed correctly." <| fun _ ->
            let config = parseFornaxConfiguration configOnlyExcludeTwoItems
            Expect.equal 2 config.Exclude.Length "The config should contain two exclusions."

        testCase "That a config with a style entry is parsed correctly." <| fun _ ->
            let config = parseFornaxConfiguration configExcludesAndStyleEntry
            Expect.isSome config.StyleConfiguration.Entry "There should be an entry point for the style preprocessor."
    ]