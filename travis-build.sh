#!/bin/bash
set -e
dotnet msbuild NetTopologySuite.IO.ShapeFile.sln /m "/t:Restore;Build" /p:Configuration=Release "/p:FrameworkPathOverride=$(dirname $(which mono))/../lib/mono/4.5/" /v:minimal /p:WarningLevel=3
dotnet test NetTopologySuite.IO.ShapeFile.Test --no-build --no-restore -c Release
