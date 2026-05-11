# AGENTS.md

Guide for human contributors and code-writing agents working in this repo.

## What this repo is

A small Windows tray utility for service-desk snippet pasting. Native .NET 8 / WinForms, no third-party NuGet dependencies. The product favors being boring and predictable over being clever.

## Layout

```
QuickReply.sln
src/
  QuickReply/         the tray app (the actual product)
  QuickReplySetup/    the first-time setup wizard (downloads the tray app from a GitHub release)
.github/workflows/    CI and release automation
README.md             user-facing docs
LICENSE               MIT
```

The setup wizard shares one file with the main app (`Theme.cs`) via `<Compile Include="..\QuickReply\Theme.cs" Link="Theme.cs" />`. Everything else is its own.

## Design rules that are not negotiable

1. **No third-party NuGet packages.** This is a deliberate supply-chain choice. If a feature seems to require one, ask first. There is almost always a `System.*` or P/Invoke path.
2. **Single-file self-contained publish.** Both projects ship as one EXE.
3. **No telemetry. No phone-home.** The only network call is the GitHub releases check, and the user can turn it off.
4. **User data lives in `%APPDATA%\QuickReply`.** Not next to the EXE. The only file that lives next to the EXE is `portable.flag` (which opts data back into being EXE-adjacent for portable installs).
5. **Hotkey paths must never deadlock the UI thread.** Hotkey events are message-pump events.
6. **AutoPaste is best-effort. Copy Only is the always-safe path.** Treat them this way in code, copy, and docs.

## Build

Requires the .NET 8 SDK.

```pwsh
dotnet build QuickReply.sln -c Release
```

Run the tray app:

```pwsh
dotnet run --project src/QuickReply/QuickReply.csproj -c Release
```

Publish single-file binaries (matches what CI ships):

```pwsh
dotnet publish src/QuickReply/QuickReply.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish

dotnet publish src/QuickReplySetup/QuickReplySetup.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish-setup
```

## Release

Releases are produced by `.github/workflows/release.yml` on `v*` tag push. The workflow:

1. Builds both projects with the publish flags above.
2. Computes SHA256 of each EXE and writes `QuickReply.exe.sha256` and `QuickReplySetup.exe.sha256` next to them.
3. Creates a GitHub Release named after the tag, attaches all four files.

Both csproj `<Version>` values must match the tag (minus the `v`). Bump them together.

The setup wizard verifies the SHA256 of the EXE it downloads against the `QuickReply.exe.sha256` sidecar in the same release before launching it. Do not break this contract.

## Versioning

Semver. Breaking config or data-location changes are minor bumps; behavioral changes that need user attention are minor; bug-only fixes are patch.

Bump both csprojs and add a `What's new` entry to `README.md`.

## Adding a new setting

1. Add the property to `src/QuickReply/Models/AppSettings.cs` with a sane default.
2. Add a row to the Settings table in `README.md`.
3. Add a control to `src/QuickReply/SettingsForm.cs` so users can change it without editing JSON.
4. If the setup wizard should expose it, add it to `src/QuickReplySetup/SetupChoices.cs` and the relevant wizard page.

## Adding default snippets

Edit `src/QuickReply/snippets-defaults.json`. It is embedded as a resource at build time and used to seed `snippets.json` on first launch.

## Style

* Comments explain *why*, not *what*. Code says what.
* Public types and methods get a one-paragraph XML doc when the contract is not obvious from the signature.
* Prefer composition over inheritance for forms; the design system in `Theme.cs` gives you painted controls (`CardPanel`, `ChipButton`, `ActionButton`).
* No emoji in code, commits, or docs.
* No em dashes in user-facing copy (README, in-app labels, release notes). Use a comma, a period, or a parenthetical.

## What not to do

* Do not change file locations without writing a migration and bumping the minor version.
* Do not register additional global hotkeys. The single hotkey is part of the product.
* Do not add network calls beyond the existing GitHub API check.
* Do not add an auto-installer. Users open the GitHub release page themselves.
* Do not silently swallow `snippets.json` parse failures: surface skipped entries.
