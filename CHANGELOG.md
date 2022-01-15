# Changelog

## 0.13.1 - 24.04.2020

### ADDED
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