@echo off

@where /q nuget
@IF ERRORLEVEL 1 (
	powershell -Command "Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile nuget.exe"
)

setlocal
set target=%~dp0\.paket
set paket=%target%\paket.exe

if not exist %paket% (
    rmdir /q /s %temp%\nuget\paket.bootstrapper 2> nul
    nuget install -out %temp%\nuget -excludeversion paket.bootstrapper
    xcopy %temp%\nuget\paket.bootstrapper\tools\* %target%\
    move %target%\paket.bootstrapper.exe %paket%
)

%paket% %*
endlocal