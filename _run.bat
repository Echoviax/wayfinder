@echo off
:: Set the startup hook to the absolute path of the patcher DLL in the current folder
set DOTNET_STARTUP_HOOKS=%~dp0RuntimePatcher.dll

:: Start the game and immediately close the command prompt window
start "" "%~dp0Neverway.exe"