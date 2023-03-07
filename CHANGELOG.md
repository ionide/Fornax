# Changelog


## 0.15.1-beta001 - 07.03.2023

### Fixed
* [Support file with spaces in the name](https://github.com/ionide/Fornax/pull/116) (thanks @MangelMaxime!)

### Changed
* [Add colors to error/success/info messages](https://github.com/ionide/Fornax/pull/118) (thanks @drewknab!)
* [Update to .NET 6, use project-based FAKE build instead of script](https://github.com/ionide/Fornax/pull/122) (thanks @kMutagene!)

## 0.14.3 - 07.05.2022

### Fixed

* [improved support for watch mode](https://github.com/ionide/Fornax/pull/103) (thanks @bigjonroberts!)

## 0.14.1 - 29.04.2022

### Added

* [A new case for the `HtmlElement` type was added for custom tags](https://github.com/ionide/Fornax/pull/106) (thanks @Freymaurer!)

## 0.14.0 - 15.01.2022

### Added

* Update to FCS 41
* Add Sourcelink to the core library
* Support .NET 5 and .NET 6
* typo fix for the About page
* Trim file paths with a platform-agnostic path separator (#91)
* Add some Generator documentation.
* Minor readme grammatical fixes (#83)
* Misc fixes (#80)
* Watch mode enhancement (#79)
* Update to .NET 5 (#78)
* Change after testing on Windows Directory.Delete throws exception on files with read-only attribute.
* Add LibGit2Sharp, add sub commands to fornax new
* Update FCS, fake-cli and paket. (#73)
* Add GitHub Actions build (#74)
* Improve mobile layout
* allow the user to configure pages size in the global loader
* Add support for paging
* Improve perf by caching generators (#65)
* Default template does not support on sub-dirs (#64)
* fix typo (#62)

## 0.13.1 - 24.04.2020

### Added
* Update to FCS 35.0 (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Pass `WATCH` define in case of watch mode (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## 0.13.0 - 20.04.2020
### Added
* Add way to check developments to the template (by [@robertpi](https://github.com/robertpi))
* Summarize posts using more marker (by [@robertpi](https://github.com/robertpi))
* Update to FCS 34.1 (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## 0.12.0 - 14.04.2020
### Added
* WebSocket refresh uses excesive CPU (by [@robertpi](https://github.com/robertpi))
* Allow generate to return a byte array (by [@robertpi](https://github.com/robertpi))
* Refactor template (by [@robertpi](https://github.com/robertpi))

## 0.11.1 - 07.04.2020
### Added
* Fix for once generator running even if not found (by [@sasmithjr](https://github.com/sasmithjr))
* Use exceptions .ToString() when printing error (by [@robertpi](https://github.com/robertpi))

## 0.11.0 - 02.03.2020
### Added
* Add a port argument to the watch command  (by [@toburger](https://github.com/toburger))
* Collect and propogate loader errors (by [@jbeeko](https://github.com/jbeeko))
* Add MultipleFiles as possible generator output (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## 0.10.0 - 20.02.2020
### Added
* Update to .NET Core 3.0
* Adds tools manifest .config/dotnet-tools.json
* Redesign Fornax around idea of `loaders` and `generators`
* Create new `fornax new` template

## 0.2.0 - 05.08.2019
### Added
* Update to .Net Core
* Distribute as .Net Core global tool

## 0.1.0 - 04.06.2017
### Added
* Defining templates in F# DSL
* Creating pages using templates from `.md` files with `layout` entry
* Creating plain pages without templates from `md` files without `layout` entry
* Transforming `.less` files to `.css` files
* Transforming `.scss` files to `.css` files (require having `sass` installed)
* Coping other static content to output directory