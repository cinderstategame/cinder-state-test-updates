@echo off
setlocal

set "SCRIPT_DIR=%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%tools\Publish-CinderStateBuild.ps1" -AutoIncrement
set "RESULT=%errorlevel%"

echo.
if not "%RESULT%"=="0" (
    echo Publish failed.
    pause
    exit /b %RESULT%
)

echo Publish complete.
pause
