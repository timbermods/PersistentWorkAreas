# Developing Persistent Work Areas

The [README](README.md) and the [website](https://timbermods.github.io/PersistentWorkAreas/) are for players. This page is for building and checking the mod.

## How it works

It is a local display only. It does not patch game methods, change simulation or building data, send multiplayer events, or modify saves; the pins live in their own small local file. It uses the same navigation queries as the game's selected-building visualizer and a separate instance of its outline renderer. The renderer reflection is isolated in `source/NativeOutline.cs`; if it fails, pinning turns off for that map only.

The BeaverBuddies Stability Fork's co-op join check compares only the game and BeaverBuddies builds, so this mod doesn't affect joining.

[VALIDATION.md](VALIDATION.md) has the details: the pin file, refresh planning, what the checks cover and the release playtest checklist.

## Build from source

Install the .NET 8 SDK and have Timberborn installed, then run:

```powershell
.\build.ps1 -GameDir 'C:\Program Files (x86)\Steam\steamapps\common\Timberborn'
```

The script builds the mod, runs every check, and creates `dist\PersistentWorkAreas-v<version>.zip`, with the version from `manifest.json`. No game, Unity, Harmony, or BeaverBuddies DLLs are redistributed. The game DLLs are used only as build references.

Without the game, `dotnet run --project tests/Checks.csproj -c Release` runs only the checks that need no game files (pin, refresh-planning, planting-tool and pin-file logic) and reports the rest as skipped. GitHub Actions runs them for pull requests and pushes to `main`.

## Text and releases

All UI text is in `packaging/PersistentWorkAreas/version-1.1/Localizations/enUS_PersistentWorkAreas.csv`; the checks confirm the DLL has no hard-coded UI text.

Version history is in the GitHub release notes. When a release becomes Latest, `.github/workflows/latest-release.yml` updates the README lines ending in `<!-- latest -->` and the site's version text.
