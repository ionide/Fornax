dotnet tool uninstall -g Fornax
dotnet pack -c Release -o nupkg
dotnet tool install --add-source ./nupkg -g fornax