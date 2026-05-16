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

Publish a tester-ready launcher that does not require testers to install .NET:

```powershell
dotnet publish .\src\CinderStateLauncher\CinderStateLauncher.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish\win-x64
```

Install the published launcher to your own Desktop for a local test:

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
