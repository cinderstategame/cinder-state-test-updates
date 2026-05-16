param(
    [string]$Source = (Join-Path $PSScriptRoot "..\publish\win-x64")
)

$ErrorActionPreference = "Stop"

$sourcePath = Resolve-Path $Source
$desktop = [Environment]::GetFolderPath("DesktopDirectory")
$installPath = Join-Path $desktop "Cinder State Launcher"
$exePath = Join-Path $installPath "CinderStateLauncher.exe"

if (!(Test-Path (Join-Path $sourcePath "CinderStateLauncher.exe"))) {
    throw "Missing CinderStateLauncher.exe in $sourcePath. Build/publish the launcher first."
}

if (Test-Path $installPath) {
    Remove-Item -LiteralPath $installPath -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $installPath | Out-Null
Copy-Item -Path (Join-Path $sourcePath "*") -Destination $installPath -Recurse -Force

$shortcutPath = Join-Path $desktop "Cinder State Launcher.lnk"
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $installPath
$shortcut.Description = "Cinder State Test Launcher"
$shortcut.Save()

Write-Host "Installed launcher to: $installPath"
Write-Host "Desktop shortcut: $shortcutPath"
