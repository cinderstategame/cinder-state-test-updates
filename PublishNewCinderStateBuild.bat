@echo off
setlocal

set "SCRIPT_DIR=%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%tools\Publish-CinderStateBuild.ps1" -AutoIncrement -UseR2 -R2Remote "cinder-state-r2" -R2Bucket "cinder-state-launcher" -R2PublicUrl "https://pub-f2c866d0b48d44e2a73269c91af359b0.r2.dev" -R2ObjectName "Cinder_State_Friend_Client_Win64.zip"
set "RESULT=%errorlevel%"

echo.
if not "%RESULT%"=="0" (
    echo Publish failed.
    pause
    exit /b %RESULT%
)

echo Publish complete.
pause
