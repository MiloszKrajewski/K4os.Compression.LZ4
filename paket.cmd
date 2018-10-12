@echo off

setlocal
set target=%~dp0\.paket
set paket=%target%\paket.exe

if not exist %paket% (
    rmdir /q /s %temp%\nuget\paket.bootstrapper 2> nul
    call %~dp0\nuget install -out %temp%\nuget -excludeversion paket.bootstrapper
    xcopy %temp%\nuget\paket.bootstrapper\tools\* %target%\
    move %target%\paket.bootstrapper.exe %paket%
)

%paket% %*
endlocal