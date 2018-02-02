@echo off

setlocal
set target=%~dp0\.fake
set fake=%target%\fake.exe

if not exist %fake% (
    rmdir /q /s %temp%\nuget\fake 2> nul
    nuget install -out %temp%\nuget -excludeversion fake
    xcopy %temp%\nuget\fake\tools\* %target%\
	rmdir /q /s %temp%\nuget\fake 2> nul
)

%fake% %*
endlocal