#r "./packages/FAKE/tools/FakeLib.dll"

open System
open System.IO
open Fake
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.Testing.Expecto

let project = "Fornax"
let summary = "Fornax is a static site generator using type safe F# DSL to define page templates"
let release = LoadReleaseNotes "CHANGELOG.md"
let buildDir  = "./temp/build/"
let appReferences = !!  "src/**/*.fsproj"
let releaseDir  = "./temp/release/"
let releaseBinDir = "./temp/release/bin/"
let releaseReferences = !! "src/**/Fornax.fsproj"

let buildTestDir  = "./temp/build_test/"
let testReferences = !!  "test/**/*.fsproj"

let testExecutables = !! (buildTestDir + "*Tests.exe")


// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; releaseDir; releaseBinDir; buildTestDir]
)

let (|Fsproj|Csproj|Vbproj|) (projFileName:string) =
    match projFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | f when f.EndsWith("csproj") -> Csproj
    | f when f.EndsWith("vbproj") -> Vbproj
    | _                           -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
    let getAssemblyInfoAttributes projectName =
        [ Attribute.Title (projectName)
          Attribute.Product project
          Attribute.Description summary
          Attribute.Version release.AssemblyVersion
          Attribute.FileVersion release.AssemblyVersion ]

    let getProjectDetails projectPath =
        let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          System.IO.Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    appReferences
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, projectName, folderName, attributes) ->
        match projFileName with
        | Fsproj -> CreateFSharpAssemblyInfo (folderName @@ "AssemblyInfo.fs") attributes
        | Csproj -> CreateCSharpAssemblyInfo ((folderName @@ "Properties") @@ "AssemblyInfo.cs") attributes
        | Vbproj -> CreateVisualBasicAssemblyInfo ((folderName @@ "My Project") @@ "AssemblyInfo.vb") attributes
        )
)


Target "Build" (fun _ ->
    MSBuildDebug buildDir "Build" appReferences
    |> Log "AppBuild-Output: "
)

Target "BuildTest" (fun _ ->
    MSBuildDebug buildTestDir "Build" testReferences
    |> Log "AppBuild-Output: "
)

Target "RunTest" (fun _ ->
    testExecutables
    |> Expecto id
)


Target "BuildRelease" (fun _ ->
    MSBuildRelease releaseDir "Build" releaseReferences
    |> Log "AppBuild-Output: "

    !! (releaseDir + "*.xml")
    ++ (releaseDir + "*.pdb")
    |> DeleteFiles

    !! (releaseDir + "*.dll")
    |> Seq.iter (MoveFile releaseBinDir)
)

// Build order
"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "BuildTest"
  ==> "RunTest"
  ==> "BuildRelease"

// start build
RunTargetOrDefault "BuildRelease"
