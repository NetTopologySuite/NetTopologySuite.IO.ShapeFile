#!/bin/bash
set -e
dotnet build -c Release
dotnet test NetTopologySuite.IO.ShapeFile.Test --no-build --no-restore -c Release
