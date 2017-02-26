module Tests

open Expecto


[<Tests>]
let modelTests =
    testList "Test Model" [
        testCase "Css property - string" <| fun _ ->
            let actual = Flex "test" |> CSSProperties.ToString
            let expected = "flex: test;"
            "CSS property with string value"
            |> Expect.equal actual expected

        testCase "Css property - float" <| fun _ ->
            let actual = ColumnCount 1. |> CSSProperties.ToString
            let expected = "column-count: 1;"
            "CSS property with float value"
            |> Expect.equal actual expected

        testCase "Html property - bool" <| fun _ ->
            let actual = DefaultChecked true |> HtmlProperties.ToString
            let expected = "defaultChecked=\"true\""
            "Html property with bool value"
            |> Expect.equal actual expected

        testCase "Html property - string" <| fun _ ->
            let actual = DefaultValue "test" |> HtmlProperties.ToString
            let expected = "defaultValue=\"test\""
            "Html property with string value"
            |> Expect.equal actual expected

        testCase "Html property - float" <| fun _ ->
            let actual = Cols 1. |> HtmlProperties.ToString
            let expected = "cols=\"1\""
            "Html property with float value"
            |> Expect.equal actual expected

        testCase "Html element - empty" <| fun _ ->
            let actual = Html.a [] [] |> HtmlElement.ToString
            let expected = "<a></a>"
            "Html element with no properties and no children"
            |> Expect.equal actual expected

        testCase "Html element - one property" <| fun _ ->
            let actual = Html.a [ Href "index.html" ] [] |> HtmlElement.ToString
            let expected = "<a href=\"index.html\"></a>"
            "Html element with one property and no children"
            |> Expect.equal actual expected

        testCase "Html element - multiple properties" <| fun _ ->
            let actual = Html.a [ Href "index.html"; Hidden true ] [] |> HtmlElement.ToString
            let expected = "<a href=\"index.html\" hidden=\"true\"></a>"
            "Html element with multiple properties and no children"
            |> Expect.equal actual expected

    ]