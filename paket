#!/usr/bin/env bash
dp0=$(cd "$(dirname "$0")" && pwd -P)
pushd $dp0
dotnet tool restore
dotnet paket "$@"
popd