# Build Instructions

These commands are for the launcher project, not the Unreal project.

Open PowerShell in:

```powershell
cd "C:\Users\jinxu\Desktop\Cinder State Test Launcher"
```

Build a normal debug/dev copy:

```powershell
dotnet build .\src\CinderStateLauncher\CinderStateLauncher.csproj
```

Publish a tester-ready launcher payload that does not require testers to install .NET:

```powershell
dotnet publish .\src\CinderStateLauncher\CinderStateLauncher.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish\win-x64
```

Build the official double-click installer EXE:

```powershell
dotnet publish .\src\CinderStateLauncherInstaller\CinderStateLauncherInstaller.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\dist\installer
```

The installer EXE will be:

```text
dist\installer\CinderStateLauncherInstaller.exe
```

The older script installer is kept only as a development fallback:

```powershell
.\installer\Install-CinderStateLauncher.bat
```

The installer copies the published launcher to:

```text
Desktop\Cinder State Launcher
```

It also creates this shortcut:

```text
Desktop\Cinder State Launcher.lnk
```

The launcher installs/updates the actual game client separately under:

```text
Desktop\Cinder State Test
```
