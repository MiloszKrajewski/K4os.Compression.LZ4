@echo off

setlocal

set target=%~dp0\.tools
set nuget=%target%\nuget.exe

if not exist %nuget% (
    mkdir %target%
    powershell -Command "iwr https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile \"%nuget%\""
)

%nuget% %*

endlocal