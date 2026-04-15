@echo off
set DIST=.\Dist\Linux
set SUBDIR=%DIST%\Wayfinder
set TEMP_BINS=%DIST%\temp_bins

echo [1/4] Cleaning old builds...
if exist %DIST% rd /s /q %DIST%
mkdir %SUBDIR%

echo [2/4] Publishing Launcher...
dotnet publish Wayfinder.Launcher -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true -o %DIST%

echo [3/4] Publishing Mod Components...
dotnet publish Wayfinder.Patcher -c Release --self-contained false -o %SUBDIR%
dotnet publish Wayfinder.Core -c Release --self-contained false -o %SUBDIR%

echo [4/4] Extracting Linux Runtime Components...
mkdir %TEMP_BINS%
dotnet publish Wayfinder.Patcher -c Release -r linux-x64 --self-contained true -o %TEMP_BINS%

copy %TEMP_BINS%\libclrjit.so %SUBDIR%\
copy %TEMP_BINS%\libcoreclr.so %SUBDIR%\
copy %TEMP_BINS%\libhostpolicy.so %SUBDIR%\

echo Cleaning up...
rd /s /q %TEMP_BINS%

if exist %SUBDIR%\Wayfinder.Patcher del /f /q %SUBDIR%\Wayfinder.Patcher
if exist %SUBDIR%\Wayfinder.Core del /f /q %SUBDIR%\Wayfinder.Core
if exist %SUBDIR%\*.json del /f /q %SUBDIR%\*.json

echo Done! Build located in %DIST%
pause