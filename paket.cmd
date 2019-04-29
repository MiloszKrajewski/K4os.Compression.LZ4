@echo off
setlocal
pushd %~dp0
set paket=.paket\paket.exe
if not exist %paket% (dotnet tool install paket --tool-path %paket%\..)
%paket% %*
popd
endlocal