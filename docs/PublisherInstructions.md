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

2. Pick the next version tag.

Example:

```text
v0.1.1
```

3. Upload the ZIP as a GitHub Release asset.

Preferred PowerShell command:

```powershell
gh release create v0.1.1 `
  "C:\Users\jinxu\Documents\Unreal Projects\Cinder_State\Saved\FriendClientPackage\Cinder_State_Friend_Client_Win64.zip" `
  --repo cinderstategame/cinder-state-test-updates `
  --title "Cinder State Test v0.1.1" `
  --notes "Private tester build v0.1.1."
```

If you use the GitHub website, upload the ZIP in the Release asset area, not inside the description text box.

4. Get the SHA256 hash.

```powershell
Get-FileHash "C:\Users\jinxu\Documents\Unreal Projects\Cinder_State\Saved\FriendClientPackage\Cinder_State_Friend_Client_Win64.zip" -Algorithm SHA256
```

5. Update root `version.json`.

Example:

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

6. Commit and push `version.json`.

```powershell
git add version.json
git commit -m "Publish Cinder State test build v0.1.1"
git push
```

Once `version.json` is pushed, testers only need to open the launcher and click update/play.

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
