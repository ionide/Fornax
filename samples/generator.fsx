#load "../paket-files/include-scripts/net46/include.main.group.fsx"
#r "../build/Fornax.dll"
let path = System.IO.Path.GetFullPath "samples"
Fornax.generateFolder path