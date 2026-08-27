# Stacks

A single-file Windows TUI launcher for a folder of locally installed games. Drop `stacks.exe` at the root of a games directory; each immediate subfolder that contains a `stacks.json` manifest becomes a game in the list.

End users do not need .NET installed — the published binary is a self-contained single-file executable.

## Manifest schema (`stacks.json`)

Place a `stacks.json` next to each game inside its own subfolder:

```json
{
  "name": "Display Name",
  "version": "1.0.0",
  "installer": "setup.exe",
  "installArgs": "",
  "launch": "bin/game.exe",
  "launchArgs": "",
  "workingDir": "bin",
  "installedMarker": ".installed",
  "notes": "Free text shown in the detail view."
}
```

| Field             | Required | Notes                                                                                                |
|-------------------|----------|------------------------------------------------------------------------------------------------------|
| `name`            | yes      | Display name in the list and detail view.                                                            |
| `launch`          | yes      | Relative path to the executable launched by `L`.                                                     |
| `version`         | no       | Free-form version string.                                                                            |
| `installer`       | no       | Relative path to the installer launched by `I`. If absent, `I` is a no-op for that game.             |
| `installArgs`     | no       | Args passed to the installer.                                                                        |
| `launchArgs`      | no       | Args passed to the launch target.                                                                    |
| `workingDir`      | no       | Working directory for the launch process. Relative to the game folder. Defaults to the folder root.  |
| `installedMarker` | no       | Sentinel file written on successful install. If unset, "installed" is inferred from `launch` existing. |
| `notes`           | no       | Markdown-free text shown in the detail view.                                                         |

Unknown fields are silently ignored, so future versions can add keys without breaking older `stacks.exe` builds.

## Key bindings

| Key       | Action                                |
|-----------|---------------------------------------|
| `↑` / `↓` | Move selection                        |
| `PgUp` / `PgDn` | Jump 10 rows                    |
| `Home` / `End` | Jump to first / last row         |
| `Enter`   | Detail view (any key returns)         |
| `I`       | Run installer (Esc to cancel)         |
| `L`       | Launch — TUI returns to the list immediately and updates state when the game exits |
| `O`       | Open game folder in Explorer          |
| `R`       | Rescan subfolders                     |
| `Q` / `Esc` / `Ctrl+C` | Quit                     |

## State file

`stacks.state.json` is written next to `stacks.exe`. It records `lastPlayed` (UTC ISO 8601) and `playCount` per folder. `playCount` is incremented when a launch starts (so a crashed game still counts).

## Building

Prerequisite: **.NET 10 SDK** (matching one of the `Microsoft.NETCore.App 10.x` runtimes). `dotnet --list-sdks` must report a 10.x SDK.

```powershell
dotnet build
dotnet test
dotnet run --project src/Stacks
```

## Publishing

The release artifact is a self-contained single-file exe (the .NET runtime and native dependencies are bundled inside):

```powershell
dotnet publish src/Stacks/Stacks.csproj -c Release -r win-x64
```

The single binary lands at:

```
src/Stacks/bin/Release/net10.0-windows/win-x64/publish/stacks.exe
```

Drop it into the root of a games directory:

```powershell
Copy-Item .\src\Stacks\bin\Release\net10.0-windows\win-x64\publish\stacks.exe D:\Games\
```

On first launch, the bundled native libraries (Skia, HarfBuzz, ANGLE) are extracted to a per-user cache under `%LOCALAPPDATA%\Temp\.net\stacks\` and reused on subsequent launches.

## Conventions worth preserving

- All JSON serialization goes through the source-generated contexts in `Manifest/ManifestJsonContext.cs` and `State/StateJsonContext.cs`. They give faster startup and zero reflection — keep using them.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is on; new warnings fail the build.
