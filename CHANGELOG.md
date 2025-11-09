# Changelog

## [0.16.0] - 2025-11-09

### Changed
- [Update to .NET 8](https://github.com/ionide/Fornax/pull/127) (thanks @Numpsy!)
- [Allow Fornax.Core to use FSharp.Core 6.0.7 or newer](https://github.com/ionide/Fornax/pull/129) (thanks @Numpsy!)
- [feat: Add the webp mime type based on the .webp extension](https://github.com/ionide/Fornax/pull/132) (thanks @YanikCeulemans!)
- [Update FAKE to 6.1.4](https://github.com/ionide/Fornax/pull/133) (thanks @Numpsy!)

## [0.15.1] - 2023-03-07

### Fixed
- [Support file with spaces in the name](https://github.com/ionide/Fornax/pull/116) (thanks @MangelMaxime!)

### Changed
- [Add colors to error/success/info messages](https://github.com/ionide/Fornax/pull/118) (thanks @drewknab!)
- [Update to .NET 6, use project-based FAKE build instead of script](https://github.com/ionide/Fornax/pull/122) (thanks @kMutagene!)

## [0.14.3] - 2022-05-07

### Fixed

- [improved support for watch mode](https://github.com/ionide/Fornax/pull/103) (thanks @bigjonroberts!)

## [0.14.1] - 2022-04-29

### Added

- [A new case for the `HtmlElement` type was added for custom tags](https://github.com/ionide/Fornax/pull/106) (thanks @Freymaurer!)

## [0.14.0] - 2022-01-15

### Added

- Update to FCS 41
- Add Sourcelink to the core library
- Support .NET 5 and .NET 6
- typo fix for the About page
- [Trim file paths with a platform-agnostic path separator](https://github.com/ionide/Fornax/pull/91)
- Add some Generator documentation.
- [Minor readme grammatical fixes](https://github.com/ionide/Fornax/pull/83)
- [Misc fixes](https://github.com/ionide/Fornax/pull/80)
- [Watch mode enhancement](https://github.com/ionide/Fornax/pull/79)
- [Update to .NET 5](https://github.com/ionide/Fornax/pull/78)
- Change after testing on Windows Directory.Delete throws exception on files with read-only attribute.
- Add LibGit2Sharp, add sub commands to fornax new
- [Update FCS, fake-cli and paket.](https://github.com/ionide/Fornax/pull/73)
- [Add GitHub Actions build](https://github.com/ionide/Fornax/pull/74)
- Improve mobile layout
- allow the user to configure pages size in the global loader
- Add support for paging
- [Improve perf by caching generators](https://github.com/ionide/Fornax/pull/65)
- [Default template does not support on sub-dirs](https://github.com/ionide/Fornax/pull/64)
- [fix typo](https://github.com/ionide/Fornax/pull/62)

## [0.13.1] - 2020-04-24

### Added
- Update to FCS 35.0 (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
- Pass `WATCH` define in case of watch mode (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## [0.13.0] - 2020-04-20
### Added
- Add way to check developments to the template (by [@robertpi](https://github.com/robertpi))
- Summarize posts using more marker (by [@robertpi](https://github.com/robertpi))
- Update to FCS 34.1 (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## [0.12.0] - 2020-04-14
### Added
- WebSocket refresh uses excesive CPU (by [@robertpi](https://github.com/robertpi))
- Allow generate to return a byte array (by [@robertpi](https://github.com/robertpi))
- Refactor template (by [@robertpi](https://github.com/robertpi))

## [0.11.1] - 2020-04-07
### Added
- Fix for once generator running even if not found (by [@sasmithjr](https://github.com/sasmithjr))
- Use exceptions .ToString() when printing error (by [@robertpi](https://github.com/robertpi))

## [0.11.0] - 2020-03-02
### Added
- Add a port argument to the watch command  (by [@toburger](https://github.com/toburger))
- Collect and propogate loader errors (by [@jbeeko](https://github.com/jbeeko))
- Add MultipleFiles as possible generator output (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## [0.10.0] - 2020-02-20
### Added
- Update to .NET Core 3.0
- Adds tools manifest .config/dotnet-tools.json
- Redesign Fornax around idea of `loaders` and `generators`
- Create new `fornax new` template

## [0.2.0] - 2019-08-05
### Added
- Update to .Net Core
- Distribute as .Net Core global tool

## [0.1.0] - 2017-06-04
### Added
- Defining templates in F# DSL
- Creating pages using templates from `.md` files with `layout` entry
- Creating plain pages without templates from `md` files without `layout` entry
- Transforming `.less` files to `.css` files
- Transforming `.scss` files to `.css` files (require having `sass` installed)
- Coping other static content to output directory