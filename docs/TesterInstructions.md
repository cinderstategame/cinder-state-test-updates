# Tester Instructions

Testers do not need Unreal Engine, Git, .NET, PowerShell commands, or batch files.

## First Install

1. Download the installer EXE you send them.
2. Double-click:

```text
CinderStateLauncherInstaller.exe
```

3. If Windows SmartScreen warns because the installer is unsigned, choose `More info` and `Run anyway`.
4. Click `Install`.
5. Open the Desktop or Start Menu shortcut:

```text
Cinder State Launcher
```

6. Click `Install and Play`.

## Normal Use

After the first install, testers should launch from:

```text
Desktop\Cinder State Launcher.lnk
```

The launcher installs and updates the game files in:

```text
%LOCALAPPDATA%\Cinder State Test
```

Do not ask testers to manually edit files in that folder. If a build is broken, publish a fixed build and update `version.json`.

The launcher itself installs to:

```text
%LOCALAPPDATA%\Cinder State Launcher
```

The installer also creates a Start Menu folder:

```text
Start Menu\Programs\Cinder State
```

To uninstall, use the Start Menu uninstall shortcut or run:

```text
%LOCALAPPDATA%\Cinder State Launcher\Uninstall Cinder State Launcher.bat
```

If upgrading from an older Desktop-based install, testers can delete these old folders after the new launcher works:

```text
Desktop\Cinder State Launcher
Desktop\Cinder State Test
```

## Failure Behavior

If a download fails, the launcher shows an error and keeps the previous working install. It does not require testers to reinstall Unreal Engine or Git.
