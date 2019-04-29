@echo off
setlocal
pushd %~dp0
set fake=.fake\fake.exe
if not exist %fake% (dotnet tool install fake-cli --tool-path %fake%\..)
%fake% build -t %*
popd
endlocal