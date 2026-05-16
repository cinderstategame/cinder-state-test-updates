# Cinder State Test Launcher

Small external Windows launcher/updater for private Cinder State tester builds.

The launcher:

- Fetches hosted update info from `version.json`.
- Compares it against the local installed version.
- Downloads the packaged client ZIP when an update is available.
- Verifies the ZIP with SHA256 when provided.
- Extracts into the tester's Desktop install folder.
- Launches `Cinder_State.exe` with the configured server args.

## Project Layout

- `src/CinderStateLauncher` - C# WinForms launcher source.
- `version.json` - live update metadata intended to be hosted from this repo.
- `version.example.json` - generic example update metadata.
- `installer` - simple installer wrapper for a published launcher build.
- `docs/BuildInstructions.md` - how to build/publish the launcher.
- `docs/TesterInstructions.md` - what testers do.
- `docs/PublisherInstructions.md` - how to publish a new Cinder State build.

## Hosted Version File

The launcher currently reads:

```text
https://raw.githubusercontent.com/cinderstategame/cinder-state-test-updates/main/version.json
```

That file points to the current GitHub Release ZIP.
