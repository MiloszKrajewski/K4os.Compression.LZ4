@echo off
pushd %~dp0
dotnet tool restore
dotnet paket %*
popd
