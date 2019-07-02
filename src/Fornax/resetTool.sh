#!/bin/sh

dotnet tool uninstall -g Fornax
dotnet pack -c release -o nupkg
dotnet tool install --add-source ./nupkg -g fornax
echo "Finished fornax reset"