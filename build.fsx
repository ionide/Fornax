// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------
#r "paket: groupref build //"
#load ".fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "netstandard"
  #r "Facades/netstandard.dll"
#endif

open Fake.Core
open Fake.DotNet
open Fake.Tools
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Fake.Api

// --------------------------------------------------------------------------------------
// Information about the project to be used at NuGet and in AssemblyInfo files
// --------------------------------------------------------------------------------------

let project = "Fornax"
let summary = "Fornax is a static site generator using type safe F# DSL to define page layouts"

let gitOwner = "Ionide"
let gitHome = "https://github.com/" + gitOwner
let gitName = "Fornax"
let gitRaw = Environment.environVarOrDefault "gitRaw" ("https://raw.github.com/" + gitOwner)

// --------------------------------------------------------------------------------------
// Build variables
// --------------------------------------------------------------------------------------

System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let packageDir = __SOURCE_DIRECTORY__ </> "out"
let buildDir = __SOURCE_DIRECTORY__ </> "temp"


// --------------------------------------------------------------------------------------
// Helpers
// --------------------------------------------------------------------------------------
let isNullOrWhiteSpace = System.String.IsNullOrWhiteSpace

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    let r =
        Command.RawCommand (cmd, arguments)
        |> CreateProcess.fromCommand
        |> CreateProcess.withWorkingDirectory workingDir
        |> Proc.run
    if r.ExitCode <> 0 then
        failwithf "Error while running '%s' with args: %s" cmd args

let getBuildParam = Environment.environVar

let DoNothing = ignore
// --------------------------------------------------------------------------------------
// Build Targets
// --------------------------------------------------------------------------------------

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [buildDir; packageDir]
)

Target.create "Restore" (fun _ ->
    DotNet.restore id ""
)

Target.create "Build" (fun _ ->
    DotNet.build id ""
)

Target.create "Publish" (fun _ ->
    DotNet.publish (fun p -> {p with OutputPath = Some buildDir}) "src/Fornax"
)

Target.create "Test" (fun _ ->
    runTool "dotnet" @"run --project .\test\Fornax.Core.UnitTests\Fornax.Core.UnitTests.fsproj" "."
)

Target.create "TestTemplate" (fun _ ->
    let templateDir = __SOURCE_DIRECTORY__ </> "src/Fornax.Template/"
    let coreDllSource = buildDir </> "Fornax.Core.dll"
    let coreDllDest = templateDir </> "_lib"  </> "Fornax.Core.dll"

    try
        System.IO.File.Copy(coreDllSource, coreDllDest, true)

        let newlyBuiltFornax = buildDir </> "Fornax.dll"

        printfn "templateDir: %s" templateDir

        runTool "dotnet" (sprintf "%s watch" newlyBuiltFornax) templateDir

    finally
        File.delete coreDllDest
)

// --------------------------------------------------------------------------------------
// Release Targets
// --------------------------------------------------------------------------------------

Target.create "Pack" (fun _ ->
    DotNet.pack (fun p ->
        { p with
            OutputPath = Some packageDir
            Configuration = DotNet.BuildConfiguration.Release
        }) "src/Fornax"
)

Target.create "Push" (fun _ ->
    let key =
        match getBuildParam "nuget-key" with
        | s when not (isNullOrWhiteSpace s) -> s
        | _ -> UserInput.getUserPassword "NuGet Key: "
    Paket.push (fun p -> { p with WorkingDir = buildDir; ApiKey = key }))

// --------------------------------------------------------------------------------------
// Build order
// --------------------------------------------------------------------------------------
Target.create "Default" DoNothing
Target.create "Release" DoNothing

"Clean"
  ==> "Restore"
  ==> "Build"
  ==> "Publish"
  ==> "Test"
  ==> "Default"

"Restore"
  ==> "Build"
  ==> "Publish"
  ==> "TestTemplate"

"Default"
  ==> "Pack"
  ==> "Push"
  ==> "Release"

Target.runOrDefault "Pack"