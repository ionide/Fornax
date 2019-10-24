# Fornax

![Logo](https://raw.githubusercontent.com/LambdaFactory/Fornax/master/logo/Fornax.png)

Fornax is a static site generator using type safe F# DSL to define page layouts.

## Working features

* Defining layouts in F# DSL
* Creating pages using templates from `.md` files with `layout` entry
* Creating plain pages without templates from `md` files without `layout` entry
* Transforming `.less` files to `.css` files
* Transforming `.scss` files to `.css` files (requires having `sass` installed)
* Copying other static content to the output directory

## Planned features

* Defining `.css` styles using F# DSL
* Handling site settings defined in multiple files (a la Jekyll's `_data` folder) (multiple models? unified model?)

## Installation

Fornax is released as a global .Net Core tool. You can install it with `dotnet tool install fornax -g`

## CLI Application

The main functionality of Fornax comes from CLI applications that lets user scaffold, and generate webpages.

* `fornax new` - scaffolds new blog in current working directory using really simple template
* `fornax build` - builds webpage, puts output to `_public` folder
* `fornax watch` - starts a small webserver that hosts your generated site, and a background process that recompiles the site whenever any changes are detected. This is the recommended way of working with Fornax.
* `fornax clean` - removes output directory and any temp files
* `fornax version` - prints out the currently-installed version of Fornax
* `fornax help` - prints out help

## Website definition

Fornax is using normal F# code (F# script files) to define layouts and data types representing content, and yaml and Markdown to provide content (fitting defined models) for the layouts. A sample website can be found in the `samples` folder - to build it, run `fornax build` in this folder.

### Site Settings

Site settings are information passed to every page during generation - every layout has access to this data.

The model representing site settings is defined in `siteModel.fsx` file in the root folder of the website, content of settings is defined in `_config.yml` file in the root folder of the website.

Sample `siteModel.fsx`:

```fsharp
type SiteModel = {
    SomeGlobalValue : string
}
```

Sample `_config.yml`:

```yml
SomeGlobalValue: "Test global value"
```

### Layouts

Layouts are F# script files representing different layouts that can be used in the website. They need to `#load` `siteModel.fsx` file, and `#r` `Fornax.Core.dll`. They need to define F# record called `Model` which defines additional settings passed to this particular layout, and `generate` function of following signature: `SiteModel -> Model -> Post list -> string -> HtmlElement`. `SiteModel` is type representing global settings of webpage, `Model` is type representing settings for this layout, `Post list` contains simplified information about all available posts in the blog (useful for navigation, creating tag cloud etc.) `string` is main content of post (already compiled to `html`).

Layouts are defined using DSL defined in `Html` module of `Fornax.Core`.

All layouts should be defined in `layouts` folder.

Sample layout:

```fsharp
#r "../lib/Fornax.Core.dll"
#load "../siteModel.fsx"

open Html
open SiteModel

type Model = {
    Name : string
    Surname : string
}

let generate (siteModel : SiteModel) (mdl : Model) (posts: Post list) (content : string) =
    html [] [
        div [] [
            span [] [ !! ("Hello world " + mdl.Name) ]
            span [] [ !! content ]
            span [] [ !! siteModel.SomeGlobalValue ]
        ]
    ]
```

### Page content

Content files are `.md` files containing page content, and a header with settings (defined using yaml). The header part is parsed, and passed to the layout's `generate` function as `Model`. The content part is compiled to html and also passed to the `generate` function. The header part needs to have the `layout` entry which defines what layout will be used for the page.

Sample page:

```markdown
---
layout: post
Name: Ja3
Surname: Ja4
---
# Something else

Some blog post written in Markdown
```

### Post list

Layouts have `Post list` as one of the input parameters that can be used for navigation, creating tag clouds etc. The `Post` is a record of the following structure:

```fsharp
type Post = {
    link : string
    title: string
    author: string option
    published: System.DateTime option
    tags: string list
    content: string
}
```

It's filled based on respective entries in `layout` part of the post content file. `link` is using name of the file - it's usually something like `\posts\post1.html`

## FAQ

1. Hmmm... it looks similar to Jekyll, doesn't it?

    * Yes, indeed. But the main advantage over Jekyll is the type safe DSL for defining layouts, which uses a normal programming language - no additional syntax to things like loops or conditional statements, it's also very easy to compose layouts - you just `#load` other layouts and execute them as normal F# functions.

2. What about F# Formatting?

    * F# Formatting is really good project, but it doesn't provide its own rendering / templating engine - it's using Razor for that. Fornax right now is handling *only* rendering / templating - hopefully, it should work pretty well as a rendering engine for F# Formatting.

## How to contribute

*Imposter syndrome disclaimer*: I want your help. No really, I do.

There might be a little voice inside that tells you you're not ready; that you need to do one more tutorial, or learn another framework, or write a few more blog posts before you can help me with this project.

I assure you, that's not the case.

This project has some clear Contribution Guidelines and expectations that you can [read here](https://github.com/LambdaFactory/Fornax/blob/master/CONTRIBUTING.md).

The contribution guidelines outline the process that you'll need to follow to get a patch merged. By making expectations and process explicit, I hope it will make it easier for you to contribute.

And you don't just have to write code. You can help out by writing documentation, tests, or even by giving feedback about this work. (And yes, that includes giving feedback about the contribution guidelines.)

Thank you for contributing!

## Contributing and copyright

The project is hosted on [GitHub](https://github.com/LambdaFactory/Fornax) where you can [report issues](https://github.com/LambdaFactory/Fornax/issues), fork
the project and submit pull requests. Please read [Contribution Guide](https://github.com/LambdaFactory/Fornax/blob/master/CONTRIBUTING.md)

The library is available under [MIT license](https://github.com/LambdaFactory/Fornax/blob/master/LICENSE.md), which allows modification and redistribution for both commercial and non-commercial purposes.

Please note that this project is released with a [Contributor Code of Conduct](CODE_OF_CONDUCT.md). By participating in this project you agree to abide by its terms.