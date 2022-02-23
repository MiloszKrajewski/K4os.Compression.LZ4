@echo off
pushd %~dp0
dotnet tool restore
dotnet fake build -t %*
popd
