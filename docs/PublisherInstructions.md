# Publisher Instructions

Use this when publishing a new private Cinder State tester build.

## Current Update Host

GitHub repo:

```text
https://github.com/cinderstategame/cinder-state-test-updates
```

Launcher version file:

```text
https://raw.githubusercontent.com/cinderstategame/cinder-state-test-updates/main/version.json
```

## Publish A New Client ZIP

1. Build/package the friend client from the Unreal project.

Current known package output:

```text
C:\Users\jinxu\Documents\Unreal Projects\Cinder_State\Saved\FriendClientPackage\Cinder_State_Friend_Client_Win64.zip
```

2. Run the automated publisher.

Easiest option: double-click this root-level file:

```text
PublishNewCinderStateBuild.bat
```

The publisher starts from `publisher-version.txt` and automatically bumps the last number by one. The first automatic publish is:

```text
0.0.0.1
```

Then:

```text
0.0.0.2
0.0.0.3
0.0.0.4
```

Command-line auto-publish option:

Example:

```powershell
cd "C:\Users\jinxu\Desktop\Cinder State Test Launcher"
.\tools\Publish-CinderStateBuild.ps1 -AutoIncrement
```

This script:

- uploads the ZIP as a GitHub Release asset,
- calculates SHA256,
- updates root `version.json`,
- commits `version.json`,
- pushes the update to GitHub.

Once `version.json` is pushed, testers only need to open the launcher and click update/play.

Use a custom note when useful:

```powershell
.\tools\Publish-CinderStateBuild.ps1 -AutoIncrement -Notes "Private tester build with updated map travel checks."
```

If you intentionally need to replace an existing release asset for the same version:

```powershell
.\tools\Publish-CinderStateBuild.ps1 -Version 0.1.1 -ReplaceExistingReleaseAsset
```

## Manual Fallback

The automated publisher writes `version.json` in this shape:

```json
{
  "version": "0.1.1",
  "downloadUrl": "https://github.com/cinderstategame/cinder-state-test-updates/releases/download/v0.1.1/Cinder_State_Friend_Client_Win64.zip",
  "exePath": "Windows/Cinder_State.exe",
  "sha256": "PUT_NEW_SHA256_HASH_HERE",
  "launchArgs": "24.61.204.60:7777 -log",
  "notes": "Private tester build v0.1.1."
}
```

## Publish A New Launcher Installer

You only need this when the launcher itself changes. Normal game updates only require a new client ZIP and `version.json` update.

1. Publish the launcher payload:

```powershell
dotnet publish .\src\CinderStateLauncher\CinderStateLauncher.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish\win-x64
```

2. Publish the installer EXE:

```powershell
dotnet publish .\src\CinderStateLauncherInstaller\CinderStateLauncherInstaller.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\dist\installer
```

3. Upload this file as a GitHub Release asset:

```text
dist\installer\CinderStateLauncherInstaller.exe
```
