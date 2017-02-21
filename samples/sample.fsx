#r "../build/Fornax.dll"

open Html

type Model = {
    Name : string
    Surname : string
}

let generate mdl =
    html [] [
        div [] [
            span [] [ string ("Hello world" + mdl.Name) ]
        ]
    ]
