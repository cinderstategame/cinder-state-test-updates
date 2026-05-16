# Tester Instructions

Testers do not need Unreal Engine, Git, or .NET when you send them a self-contained published launcher package.

## First Install

1. Extract the launcher package you send them.
2. Run:

```text
Install-CinderStateLauncher.bat
```

3. Open the Desktop shortcut:

```text
Cinder State Launcher
```

4. Click `Install and Play`.

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
