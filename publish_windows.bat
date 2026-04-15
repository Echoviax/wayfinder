@echo off
set DIST=.\Dist\Windows
set SUBDIR=%DIST%\Wayfinder

echo [1/4] Cleaning...
if exist %DIST% rd /s /q %DIST%
mkdir %SUBDIR%

echo [2/4] Publishing Launcher
dotnet publish Wayfinder.Launcher -r win-x64 -c Release /p:PublishSingleFile=true /p:SelfContained=false -o %DIST%

echo [3/4] Publishing Mod Loader...
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

echo Done! Build located in %DIST%
pause