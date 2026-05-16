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
5. Open the Desktop shortcut:

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
Desktop\Cinder State Test
```

Do not ask testers to manually edit files in that folder. If a build is broken, publish a fixed build and update `version.json`.

## Failure Behavior

If a download fails, the launcher shows an error and keeps the previous working install. It does not require testers to reinstall Unreal Engine or Git.
