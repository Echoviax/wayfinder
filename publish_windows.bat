@echo off
set DIST=.\Dist\Windows
set SUBDIR=%DIST%\Wayfinder

echo [1/3] Cleaning...
if exist %DIST% rd /s /q %DIST%
mkdir %SUBDIR%

echo [2/3] Publishing Launcher
dotnet publish Wayfinder.Launcher -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o %DIST%

echo [3/3] Publishing Mod Loader
dotnet publish Wayfinder.Patcher -c Release --self-contained false -o %SUBDIR%
dotnet publish Wayfinder.Core -c Release --self-contained false -o %SUBDIR%

echo [4/4] Grabbing Runtime Files...
mkdir %DIST%\temp_bins
dotnet publish Wayfinder.Patcher -c Release -r win-x64 --self-contained true -o %DIST%\temp_bins

copy %DIST%\temp_bins\clrjit.dll %SUBDIR%\
copy %DIST%\temp_bins\coreclr.dll %SUBDIR%\
copy %DIST%\temp_bins\hostpolicy.dll %SUBDIR%\

rd /s /q %DIST%\temp_bins
del %SUBDIR%\*.exe
del %SUBDIR%\*.json

echo Done!
pause