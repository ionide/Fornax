open Fake.DotNet
#r "paket:
nuget Fake.Core.Target
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.Testing.Expecto
nuget Fake.Core.ReleaseNotes
nuget Fake.DotNet.MsBuild
nuget Fake.DotNet.AssemblyInfoFile  //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators
open Fake.DotNet
open Fake.Core.TargetOperators
open Fake.DotNet.Testing

let project = "Fornax"
let summary = "Fornax is a static site generator using type safe F# DSL to define page templates"
let release = ReleaseNotes.load "CHANGELOG.md"
let buildDir  = "./temp/build/"
let appReferences = !!  "src/**/*.fsproj"
let releaseDir  = "./temp/release/"
let releaseBinDir = "./temp/release/bin/"
let releaseReferences = !! "src/**/Fornax.fsproj"

let templates = "./src/Fornax.Template"

let buildTestDir  = "./temp/build_test/"
let testReferences = !!  "test/**/*.fsproj"

let testExecutables = !! (buildTestDir + "*Tests.exe")


// Targets
Target.create "Clean" (fun _ ->
    Shell.cleanDirs [buildDir; releaseDir; releaseBinDir; buildTestDir]
)

let (|Fsproj|Csproj|Vbproj|) (projFileName:string) =
    match projFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | f when f.EndsWith("csproj") -> Csproj
    | f when f.EndsWith("vbproj") -> Vbproj
    | _                           -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)

let debugParams (defaults:MSBuildParams) =
    { defaults with
        Verbosity = Some(Quiet)
        Targets = ["Build"]
        Properties =
            [
                "Optimize", "True"
                "DebugSymbols", "True"
            ]
    }

let releaseParams (defaults:MSBuildParams) =
    { defaults with
        Verbosity = Some(Quiet)
        Targets = ["Build"]
        Properties =
            [
                "Optimize", "True"
                "DebugSymbols", "False"
            ]
    }

// Generate assembly info files with the right version & up-to-date information
Target.create "AssemblyInfo" (fun _ ->
    let getAssemblyInfoAttributes projectName =
        [ AssemblyInfo.Title (projectName)
          AssemblyInfo.Product project
          AssemblyInfo.Description summary
          AssemblyInfo.Version release.AssemblyVersion
          AssemblyInfo.FileVersion release.AssemblyVersion ]

    let getProjectDetails projectPath =
        let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          System.IO.Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    appReferences
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, _projectName, folderName, attributes) ->
        match projFileName with
        | Fsproj -> AssemblyInfoFile.createFSharp (folderName @@ "AssemblyInfo.fs") attributes
        | Csproj -> AssemblyInfoFile.createCSharp ((folderName @@ "Properties") @@ "AssemblyInfo.cs") attributes
        | Vbproj -> AssemblyInfoFile.createVisualBasic ((folderName @@ "My Project") @@ "AssemblyInfo.vb") attributes
        )
)


Target.create "Build" (fun _ ->
    MSBuild.runDebug debugParams buildDir "Build" appReferences
    |> Trace.logItems "AppBuild-Output: "
)

Target.create "BuildTest" (fun _ ->
    MSBuild.runDebug debugParams buildTestDir "Build" testReferences
    |> Trace.logItems "AppBuild-Output: "
)

Target.create "RunTest" (fun _ ->
    testExecutables
    |> Testing.Expecto.run id
)


Target.create "BuildRelease" (fun _ ->
    MSBuild.runRelease releaseParams buildTestDir "Build" releaseReferences
    |> Trace.logItems "AppBuild-Output: "

    !! (releaseDir + "*.xml")
    ++ (releaseDir + "*.pdb")
    |> File.deleteAll

    !! (releaseDir + "*.dll")
    |> Seq.iter (Shell.moveFile releaseBinDir)
    let projectTemplateDir = releaseDir </> "templates" </> "project"

    Shell.copyDir projectTemplateDir templates (fun _ -> true)
)

// Build order
"Clean"
    ==> "AssemblyInfo"
    ==> "Build"
    ==> "BuildTest"
    ==> "RunTest"
    ==> "BuildRelease"

// start build
Target.runOrDefault "BuildRelease"
