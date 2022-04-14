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

        testCase "Custom Html element - empty" <| fun _ ->
            let actual = Html.custom "test" [] [] |> HtmlElement.ToString
            let expected = "<test></test>"
            "Custom Html element with no properties and no children"
            |> Expect.equal actual expected

        testCase "Html element - one property" <| fun _ ->
            let actual = Html.a [ Href "index.html" ] [] |> HtmlElement.ToString
            let expected = "<a href=\"index.html\"></a>"
            "Html element with one property and no children"
            |> Expect.equal actual expected

        testCase "Custom Html element - one property" <| fun _ ->
            let actual = Html.custom "test" [ Href "index.html" ] [] |> HtmlElement.ToString
            let expected = "<test href=\"index.html\"></test>"
            "Custom Html element with one property and no children"
            |> Expect.equal actual expected

        testCase "Html element - multiple properties" <| fun _ ->
            let actual = Html.a [ Href "index.html"; Hidden true ] [] |> HtmlElement.ToString
            let expected = "<a href=\"index.html\" hidden=\"true\"></a>"
            "Html element with multiple properties and no children"
            |> Expect.equal actual expected

        testCase "Custom Html element - multiple property" <| fun _ ->
            let actual = Html.custom "test" [ Href "index.html"; Hidden true ] [] |> HtmlElement.ToString
            let expected = "<test href=\"index.html\" hidden=\"true\"></test>"
            "Custom Html element with multiple properties and no children"
            |> Expect.equal actual expected

        testCase "Html element - one child" <| fun _ ->
            let actual = Html.a [] [ Html.span [] [] ] |> HtmlElement.ToString
            let expected = "<a>\n  <span></span>\n</a>"
            "Html element with no properties and one child"
            |> Expect.equal actual expected

        testCase "Custom Html element - one child" <| fun _ ->
            let actual = Html.custom "test" [] [ Html.span [] [] ] |> HtmlElement.ToString
            let expected = "<test>\n  <span></span>\n</test>"
            "Custom Html element with no properties and one child"
            |> Expect.equal actual expected

        testCase "Html element - multiple children" <| fun _ ->
            let actual = Html.a [] [ Html.span [] []; Html.div [] [] ] |> HtmlElement.ToString
            let expected = "<a>\n  <span></span>\n  <div></div>\n</a>"
            "Html element with no properties and multiple children"
            |> Expect.equal actual expected

        testCase "Custom Html element - multiple children" <| fun _ ->
            let actual = Html.custom "test" [] [ Html.span [] []; Html.div [] [] ] |> HtmlElement.ToString
            let expected = "<test>\n  <span></span>\n  <div></div>\n</test>"
            "Custom Html element with no properties and multiple children"
            |> Expect.equal actual expected

        testCase "Html element - multiple properites and children" <| fun _ ->
            let actual = Html.a [ Href "index.html"; Hidden true] [ Html.span [] []; Html.div [] [] ] |> HtmlElement.ToString
            let expected = "<a href=\"index.html\" hidden=\"true\">\n  <span></span>\n  <div></div>\n</a>"
            "Html element with multiple properties and multiple children"
            |> Expect.equal actual expected

        testCase "Custom Html element - multiple properites and children" <| fun _ ->
            let actual = Html.custom "test" [ Href "index.html"; Hidden true] [ Html.span [] []; Html.div [] [] ] |> HtmlElement.ToString
            let expected = "<test href=\"index.html\" hidden=\"true\">\n  <span></span>\n  <div></div>\n</test>"
            "Custom Html element with multiple properties and multiple children"
            |> Expect.equal actual expected

        testCase "Html element - void element as child" <| fun _ ->
            let actual = Html.div [ ] [ Html.br [ ] ] |> HtmlElement.ToString
            let expected = "<div>\n  <br/>\n</div>"
            "Html element with one void element as child"
            |> Expect.equal actual expected

        testCase "Html element - mutliple properties and children (void and normal element)" <| fun _ ->
            let actual = Html.div [ HtmlProperties.Style [ Display "block" ] ] [ Html.br [ ]; Html.p [ ] [ ]; Html.img [ Src "https://dummyimage.com/128x128/" ] ] |> HtmlElement.ToString
            let expected = "<div style=\"display: block;\">\n  <br/>\n  <p></p>\n  <img src=\"https://dummyimage.com/128x128/\"/>\n</div>"
            "Html element with one void element as child"
            |> Expect.equal actual expected

        testCase "Custom Html element - mutliple properties and children (void and normal element)" <| fun _ ->
            let actual = Html.custom "test" [ HtmlProperties.Style [ Display "block" ] ] [ Html.br [ ]; Html.p [ ] [ ]; Html.img [ Src "https://dummyimage.com/128x128/" ] ] |> HtmlElement.ToString
            let expected = "<test style=\"display: block;\">\n  <br/>\n  <p></p>\n  <img src=\"https://dummyimage.com/128x128/\"/>\n</test>"
            "Custom Html element with one void element as child"
            |> Expect.equal actual expected

        testCase "Custom Html element - as child with property and child" <| fun _ ->
            let actual = Html.div [] [ Html.custom "test" [ HtmlProperties.Style [ Display "block" ] ] [ Html.span [] [] ] ] |> HtmlElement.ToString
            let expected = "<div>\n  <test style=\"display: block;\">\n    <span></span>\n  </test>\n</div>"
            "Custom Html element with one void element as child"
            |> Expect.equal actual expected

        testCase "Html void element - empty" <| fun _ ->
            let actual = Html.br [ ] |> HtmlElement.ToString
            let expected = "<br/>"
            "Html void element with not properties"
            |> Expect.equal actual expected

        testCase "Html void element - one property" <| fun _ ->
            let actual = Html.img [ Src "https://dummyimage.com/128x128/" ] |> HtmlElement.ToString
            let expected = "<img src=\"https://dummyimage.com/128x128/\"/>"
            "Html void element with one property"
            |> Expect.equal actual expected

        testCase "Html void element - multiple properties" <| fun _ ->
            let actual = Html.img [ Src "https://dummyimage.com/128x128/"; Alt "A dummy image of 128 by 128 pixels"] |> HtmlElement.ToString
            let expected = "<img src=\"https://dummyimage.com/128x128/\" alt=\"A dummy image of 128 by 128 pixels\"/>"
            "Html void element with multiple properties"
            |> Expect.equal actual expected

    ]