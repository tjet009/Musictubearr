#!/bin/sh
set -e
exec dotnet /app/Lidarr.dll -nobrowser -data=/config "$@"
