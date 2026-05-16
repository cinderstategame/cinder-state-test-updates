@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-CinderStateLauncher.ps1"
if errorlevel 1 (
    echo.
    echo Launcher install failed.
    pause
    exit /b %errorlevel%
)

echo.
echo Launcher installed.
pause
