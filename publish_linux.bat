@echo off
set DIST=.\Dist\Linux
set SUBDIR=%DIST%\Wayfinder
set TEMP_BINS=%DIST%\temp_bins

echo [1/4] Cleaning...
if exist %DIST% rd /s /q %DIST%
mkdir %SUBDIR%

echo [2/4] Publishing Launcher...
dotnet publish Wayfinder.Launcher -r linux-x64 -c Release /p:PublishSingleFile=true /p:SelfContained=false -o %DIST%

echo [3/4] Publishing Mod Loader...
dotnet publish Wayfinder.Patcher -c Release --self-contained false -o %SUBDIR%
dotnet publish Wayfinder.Core -c Release --self-contained false -o %SUBDIR%

echo [4/4] Grabbing Runtime Files...
mkdir %TEMP_BINS%
dotnet publish Wayfinder.Patcher -c Release -r linux-x64 --self-contained true -o %TEMP_BINS%

copy %TEMP_BINS%\libclrjit.so %SUBDIR%\
copy %TEMP_BINS%\libcoreclr.so %SUBDIR%\
copy %TEMP_BINS%\libhostpolicy.so %SUBDIR%\

rd /s /q %TEMP_BINS%

if exist %SUBDIR%\Wayfinder.Patcher del /f /q %SUBDIR%\Wayfinder.Patcher
if exist %SUBDIR%\Wayfinder.Core del /f /q %SUBDIR%\Wayfinder.Core
if exist %SUBDIR%\*.json del /f /q %SUBDIR%\*.json

echo Done! Build located in %DIST%
pause