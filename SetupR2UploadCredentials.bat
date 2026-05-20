@echo off
setlocal

set "SCRIPT_DIR=%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%tools\Setup-R2RcloneRemote.ps1" -RemoteName "cinder-state-r2" -BucketName "cinder-state-launcher"
set "RESULT=%errorlevel%"

echo.
if not "%RESULT%"=="0" (
    echo R2 credential setup failed.
    pause
    exit /b %RESULT%
)

echo R2 credential setup complete.
pause
