#load "../paket-files/include-scripts/net46/include.main.group.fsx"
#r "../build/Fornax.Core.dll"
let path = System.IO.Path.GetFullPath "samples"
Generator.generateFolder path