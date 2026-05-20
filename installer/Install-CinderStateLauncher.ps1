param(
    [string]$Source = (Join-Path $PSScriptRoot "..\publish\win-x64")
)

$ErrorActionPreference = "Stop"

$sourcePath = Resolve-Path $Source
$localAppData = [Environment]::GetFolderPath("LocalApplicationData")
$desktop = [Environment]::GetFolderPath("DesktopDirectory")
$programs = [Environment]::GetFolderPath("Programs")
$installPath = Join-Path $localAppData "Cinder State Launcher"
$gameInstallPath = Join-Path $localAppData "Cinder State Test"
$exePath = Join-Path $installPath "CinderStateLauncher.exe"
$uninstallPath = Join-Path $installPath "Uninstall Cinder State Launcher.bat"
$startMenuPath = Join-Path $programs "Cinder State"

if (!(Test-Path (Join-Path $sourcePath "CinderStateLauncher.exe"))) {
    throw "Missing CinderStateLauncher.exe in $sourcePath. Build/publish the launcher first."
}

if (Test-Path $installPath) {
    Remove-Item -LiteralPath $installPath -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $installPath | Out-Null
Copy-Item -Path (Join-Path $sourcePath "*") -Destination $installPath -Recurse -Force

$uninstallLines = @(
    "@echo off",
    "setlocal EnableExtensions",
    "",
    "set `"INSTALL_DIR=%LOCALAPPDATA%\Cinder State Launcher`"",
    "set `"GAME_DIR=%LOCALAPPDATA%\Cinder State Test`"",
    "set `"DESKTOP_SHORTCUT=%USERPROFILE%\Desktop\Cinder State Launcher.lnk`"",
    "set `"DESKTOP_URL=%USERPROFILE%\Desktop\Cinder State Launcher.url`"",
    "set `"START_MENU_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Cinder State`"",
    "",
    "echo This will remove Cinder State Launcher and downloaded Cinder State Test files.",
    "choice /C YN /M `"Continue`"",
    "if errorlevel 2 exit /b 0",
    "",
    "taskkill /IM CinderStateLauncher.exe /F >nul 2>nul",
    "del /F /Q `"%DESKTOP_SHORTCUT%`" >nul 2>nul",
    "del /F /Q `"%DESKTOP_URL%`" >nul 2>nul",
    "rmdir /S /Q `"%START_MENU_DIR%`" >nul 2>nul",
    "rmdir /S /Q `"%GAME_DIR%`" >nul 2>nul",
    "cd /d `"%TEMP%`"",
    "start `"`" /min cmd /c `"timeout /t 2 /nobreak >nul & rmdir /s /q `"`"`"%INSTALL_DIR%`"`"`"`"",
    "echo Uninstall scheduled.",
    "exit /b 0"
)
Set-Content -LiteralPath $uninstallPath -Value $uninstallLines -Encoding ascii

New-Item -ItemType Directory -Force -Path $startMenuPath | Out-Null
$shell = New-Object -ComObject WScript.Shell

function New-LauncherShortcut {
    param(
        [string]$Path,
        [string]$Target,
        [string]$WorkingDirectory,
        [string]$Description
    )

    $shortcut = $shell.CreateShortcut($Path)
    $shortcut.TargetPath = $Target
    $shortcut.WorkingDirectory = $WorkingDirectory
    $shortcut.Description = $Description
    $shortcut.IconLocation = $Target
    $shortcut.Save()
}

$desktopShortcutPath = Join-Path $desktop "Cinder State Launcher.lnk"
$startMenuShortcutPath = Join-Path $startMenuPath "Cinder State Launcher.lnk"
$startMenuUninstallPath = Join-Path $startMenuPath "Uninstall Cinder State Launcher.lnk"

New-LauncherShortcut $desktopShortcutPath $exePath $installPath "Cinder State Test Launcher"
New-LauncherShortcut $startMenuShortcutPath $exePath $installPath "Cinder State Test Launcher"
New-LauncherShortcut $startMenuUninstallPath $uninstallPath $installPath "Uninstall Cinder State Launcher"

Write-Host "Installed launcher to: $installPath"
Write-Host "Game files install/update to: $gameInstallPath"
Write-Host "Desktop shortcut: $desktopShortcutPath"
Write-Host "Start Menu folder: $startMenuPath"
Write-Host "Uninstall script: $uninstallPath"
