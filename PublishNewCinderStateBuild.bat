@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "VERSION=%~1"

if "%VERSION%"=="" (
    set /p VERSION=Enter new Cinder State test build version, for example 0.1.1: 
)

if "%VERSION%"=="" (
    echo.
    echo No version entered. Nothing was published.
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%tools\Publish-CinderStateBuild.ps1" -Version "%VERSION%"
set "RESULT=%errorlevel%"

echo.
if not "%RESULT%"=="0" (
    echo Publish failed.
    pause
    exit /b %RESULT%
)

echo Publish complete.
pause
