@echo off
setlocal enabledelayedexpansion

:: Check first arg
if "%~1"=="" (
    echo [ERRO] No input file specified.
    echo Usage: Drag and drop the Neverway .exe onto this script, or run:
    echo decompile.bat "C:\path\to\your\Neverway.exe"
    pause
    exit /b 1
)

set "INPUT_FILE=%~1"
set "OUTPUT_DIR=%~dpn1_decompiled"

echo -----------------------------
echo Starting Decompilation
echo -----------------------------
echo Target: "%INPUT_FILE%"
echo Output: "%OUTPUT_DIR%"
echo.

where ilspycmd >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [INFO] ilspycmd not found. Attempting to install it automatically...
    
    where dotnet >nul 2>nul
    if !ERRORLEVEL! NEQ 0 (
        echo [ERRO] The .NET SDK is not installed or not in your PATH.
        echo Please install the .NET SDK from https://dotnet.microsoft.com/ (Preferably 8.0.xx)
        pause
        exit /b 1
    )

    dotnet tool install -g ilspycmd
    
    where ilspycmd >nul 2>nul
    if !ERRORLEVEL! NEQ 0 (
        echo [ERRO] Failed to install ilspycmd automatically. 
        pause
        exit /b 1
    )
    
    echo [OKAY] ilspycmd installed successfully!
    echo.
)

ilspycmd -o "%OUTPUT_DIR%" "%INPUT_FILE%"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [OKAY] Decompilation complete! 
    echo DLLs saved to: "%OUTPUT_DIR%"
) else (
    echo.
    echo [ERRO] ilspycmd encountered an error. 
)

pause