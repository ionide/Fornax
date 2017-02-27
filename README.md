![Logo](https://gitlab.com/Krzysztof-Cieslak/Fornax/raw/master/logo/Fornax.png)

Fornax is a static site generator using type safe F# DSL to define page templates.

## Working features

* Defining templates in F# DSL
* Creating pages using templates from `.md` files with `layout` entry
* Creating plain pages without templates from `md` files without `layout` entry
* Transforming `.less` files to `.css` files
* Coping other static content to output directory

## Plsnned features

* Defining `.css` styles using F# DSL
* Better performance (startup time in single build mode (`fornax build`) is bad)
* Better handling of site settings (right now there is no access to post list etc.) (passing `SiteSettings<SiteModel>` to generator ?)
* Handling site settings defined in multiple files (a la Jekyll's `_data` folder) (multiple models? unified model?)

## Installation.

Project is in really early stage right now... let's call it RC3. Right now only way of installing Fornax is building it from source - clone repository, and run `build.cmd` (Windows) or `build.sh` (Linux / OSX). Compailed `Fornax.exe` will be in `.\temp\release` folder, `Fornax.Core.dll` in `.\temp\release\bin`. You can add `.\temp\release` to `PATH` - Fornax is desinged as globally installed tool.

## CLI Application

TODO: More docs. For now run `fornax help`... BTW, `fornax new` is not implemented yet (but other commands should work)

## Website definition

Fornax is using normal F# code (F# script files) to define templates and data types representing content, and yaml and Markdown to provide content (fitting defined models) for the templates. Sample webpage can be found in `samples` folder - to build sample webpage run `fornax build` in this folder.

### Site Settings

Site settings are information passed to every page during generation - every template has access to this data.

The model representing site settings is defined in `siteModel.fsx` file in the root folder of the webpage, content of settings is defined in `site.yaml` file in the root folder of the webpage.

Sample `siteModel.fsx`:

```
type SiteModel = {
    SomeGlobalValue : string
}
```

Sample `site.yaml`:
```
SomeGlobalValue: "Test global value"
```

### Templates

Templates are F# script files representing different templates that can be used in the website. They need to `#load` `siteModel.fsx` file, and `#r` `Fornax.Core.dll`. They need to define F# record called `Model` which defines additional settings passed to this particular template, and `generate` function of following signature: `SiteModel -> Model -> string -> HtmlElement`. `SiteModel` is type representing global settings of webpage, `Model` is type representing settings for this template, `string` is main content of post (already compiled to `html`).

Templates are defined using DSL defined in `Html` module of `Fornax.Core`.

All templates should be defined in `templates` folder.

Sample template:
```
#r "../lib/Fornax.Core.dll"
#load "../siteModel.fsx"

open Html
open SiteModel

type Model = {
    Name : string
    Surname : string
}

let generate (siteModel : SiteModel) (mdl : Model) (content : string) =
    html [] [
        div [] [
            span [] [ !! ("Hello world " + mdl.Name) ]
            span [] [ !! content ]
            span [] [ !! siteModel.SomeGlobalValue ]
        ]
    ]
```

### Page content

Content files are `.md` files containg page contnt, and header with settings (defined using yaml). Header part is parsed, and passed to template `generate` function as `Model`. Content part is compiled to html and also passed to `generate` function. Header part needs to have `layout` entry which defines which template will be used for the page.

Sample page:

```
---
layout: post
Name: Ja3
Surname: Ja4
---
# Something else

Some blog post written in Markdown
```

## FAQ

1. Hmmm... it looks similar to Jekyll, doesn't it?

* Yes, indeed. But the main advantage over Jekyll is type safe DSL for defining templates, and fact it's using normal prorgramming language - no additional syntax to things like loops or conditional statements, it's also very easy to compose templates - you just `#load` other template and execute as normal F# function

2. What about F# Formatting?

* F# Formatting is really good project, but it doesn't provide own rendering / templating engine - it's using Razor for that. Fornax right now is handling *only* rendering / templating - hopefully, it should work pretty well as rendering engine for F# Formatting.

## Contributing and copyright

The project is hosted on [GitHub](https://gitlab.com/Krzysztof-Cieslak/Fornax) where you can [report issues](https://gitlab.com/Krzysztof-Cieslak/Fornax/issues), fork
the project and submit pull requests. Please read [Contribution Guide](https://gitlab.com/Krzysztof-Cieslak/Fornax/blob/master/CONTRIBUTING.md)

The library is available under [MIT license](https://gitlab.com/Krzysztof-Cieslak/Fornax/blob/master/LICENSE.md), which allows modification and redistribution for both commercial and non-commercial purposes.