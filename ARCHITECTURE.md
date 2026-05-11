# Architecture

Developer notes for working on QuickReply.

## Overview
QuickReply is a Windows-only WinForms tray utility (.NET 8) that replaces an
AutoHotkey snippet picker for service desk / ticket response use. The app
sits in the system tray, listens for a global hotkey (Ctrl+Alt+;), and opens
a styled snippet picker. The user types a short code (e.g. `fu`), previews
the matching text, and either pastes it into the previously-active window
or copies it to the clipboard.

Target framework: `net8.0-windows`. Windows only. No external NuGet packages
beyond what `Microsoft.NET.Sdk` and `UseWindowsForms` already provide.
JSON is handled with `System.Text.Json`.

## Build / run
From the repository root:

```
dotnet build QuickReply.sln -c Release
dotnet run --project src/QuickReply/QuickReply.csproj -c Release
```

Publish a self-contained single-file Windows x64 executable:

```
dotnet publish src/QuickReply/QuickReply.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## Runtime files
On first launch the app creates two files next to the executable:

- `snippets.json` — user-editable map of `code -> text`.
- `appsettings.json` — app settings (AutoPaste, RestoreClipboardAfterPaste,
  ClipboardRestoreDelayMs, PasteDelayMs, Theme, Hotkey).

When debugging via `dotnet run`, these files land in
`src/QuickReply/bin/Debug/net8.0-windows/`.

## File layout
- `Program.cs` — entry point. Enforces single instance via a named `Mutex`,
  bootstraps WinForms, runs `TrayApplicationContext`.
- `TrayApplicationContext.cs` — owns the `NotifyIcon`, the hotkey manager,
  the picker form lifetime, and the tray menu commands. Acts as the
  composition root for the services.
- `Models/AppSettings.cs` — POCO for `appsettings.json`.
- `SettingsService.cs` — load/save `appsettings.json` with defaults.
- `SnippetService.cs` — load/reload `snippets.json`, expand `{{date:FORMAT}}`
  tokens. Also exposes `AddOrUpdate(code, text)` and `Contains(code)` for the
  in-app snippet editor. Keeps the last successfully loaded snippet set if a
  reload fails.
- `AddSnippetForm.cs` — themed modal dialog for adding or editing a snippet.
  Reachable from the picker's "+ New snippet" button and the tray menu's
  "Add Snippet..." item. Detects existing codes and switches the header to
  "Edit Snippet" mode.
- `HotkeyManager.cs` — wraps `RegisterHotKey` / `UnregisterHotKey` via a
  hidden message-only `NativeWindow`. Raises `HotkeyPressed` on the UI thread.
- `ClipboardService.cs` — best-effort clipboard save/restore (text only).
- `PasteService.cs` — captures the foreground window before the picker
  opens, then restores focus and sends Ctrl+V via `SendKeys.SendWait`.
  Isolated so the send method can be replaced (SendInput) later if needed.
- `SnippetPickerForm.cs` — borderless always-on-top picker with code input,
  preview, quick-pick chips, Paste / Copy Only / Cancel, status text.
- `Theme.cs` — design tokens (palette + fonts) and the owner-drawn controls
  used by the picker: `CardPanel`, `ChipButton`, `ActionButton`, `HotkeyPill`.
  All custom controls clear to `BackColor` before painting their rounded path
  so the corners outside the path blend with the parent.
- `src/QuickReplySetup/` — a sibling WinForms project that builds a separate
  `QuickReplySetup.exe` (the first-time install wizard). It is self-contained
  .NET 8 and links the main app's `Theme.cs` so the wizard matches the picker
  visually. The installer downloads `QuickReply.exe` from
  `https://github.com/Tofu-Water-Drinker/QuickReply/releases/latest/download/QuickReply.exe`
  at install time (so the installer binary stays small relative to embedding
  the app, and always installs the latest released version). The wizard
  writes `appsettings.json` with the user's hotkey choice and, if they pick
  custom snippets, `snippets.json`. Windows startup is implemented as a
  string value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
  (no admin privileges, no shortcut file, easy to remove via Task Manager
  Startup). Update the `ReleaseDownloadUrl` constant in `Installer.cs` if
  the repository ever moves.
- `UpdateService.cs` — queries
  `https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest`,
  parses `tag_name`, strips any leading `v` and any `-prerelease` / `+meta`
  suffix, and compares against `Assembly.GetName().Version`. The version
  comparison normalises missing Build/Revision components (Version uses -1
  for absent parts) so a 3-part tag like `v1.0.0` compares cleanly against
  a 4-part AssemblyVersion like `1.0.0.0`. `TrayApplicationContext` owns the
  service; an auto-check fires 5 seconds after startup (configurable via
  `CheckForUpdatesOnStartup` in `appsettings.json`), and the
  "Check for Updates..." tray menu item runs a manual check that always
  reports the result.

## Hotkey / paste gotchas
- Default hotkey is `Ctrl+Alt+;` (`VK_OEM_1` = `0xBA`). It is configurable
  via `appsettings.json` (`"Hotkey": "Ctrl+Alt+;"`). The parser lives in
  `HotkeyManager.ParseHotkey`.
- The hotkey is registered with a hidden message-only window. If another
  process already owns the combination, `RegisterHotKey` returns false and
  the tray icon shows a balloon tip explaining the conflict.
- The hotkey **must** be unregistered on exit. `TrayApplicationContext.Dispose`
  handles it; also wired up to `Application.ApplicationExit`.
- The foreground window must be captured **before** the picker form is
  shown (the picker itself becomes foreground when shown). Currently
  captured inside the hotkey handler before calling `picker.Show()`.
- Paste flow: save clipboard → put snippet on clipboard → hide picker →
  `SetForegroundWindow(prevWindow)` → small `PasteDelayMs` wait →
  `SendKeys.SendWait("^v")` → wait `ClipboardRestoreDelayMs` →
  restore previous clipboard contents. Restore runs on a background `Task`
  so the UI thread is not blocked.
- If QuickReply is not elevated but the target window is running as
  administrator, Windows blocks `SendInput` / `SendKeys` from non-elevated
  processes (UIPI). The paste will silently fail. Copy Only still works.
  This is documented in README.md.
- Some web apps (notably certain ticketing systems) reject programmatic
  paste. `Copy Only` is the reliable fallback and must always work.

## Snippet JSON format
Flat object, `code -> text`:

```json
{
  "fu": "Following up on this ticket...",
  "date": "{{date:yyyy-MM-dd}}"
}
```

Codes are case-insensitive on lookup but stored as written. Tokens of the
form `{{date:FORMAT}}` are expanded with `DateTime.Now.ToString(FORMAT)`
at paste/copy time, not at load time, so the timestamp is current.

## Conventions
- Keep the dependency surface minimal. Don't add NuGet packages unless
  there is a concrete justification.
- Keep paste/hotkey/clipboard P/Invoke code isolated in their respective
  service files so they can be swapped (e.g. `SendInput` instead of
  `SendKeys`) without touching the UI.
- The picker uses owner-drawn controls in `Theme.cs` for rounded chips,
  cards, and buttons. Don't pull in WPF or a third-party theming library —
  if you need a new control, extend `Theme.cs` in the same style
  (override `OnPaint`, clear to `BackColor`, draw a `GraphicsPath` from
  `Theme.RoundedRect`, use `TextRenderer` for crisp text).
- The picker form is `FormBorderStyle.None` with `CS_DROPSHADOW` and
  `WS_EX_TOOLWINDOW` in `CreateParams`. It's dismissed on `Deactivate` so
  the user can click anywhere outside to close.
- Updates never auto-install. `UpdateService` only reports availability;
  the user clicks through to the GitHub releases page to download the new
  exe by hand. Self-replacing a running `.exe` is brittle on Windows and
  is intentionally out of scope.
- Bump the project version in **two places** when you cut a release:
  the `<Version>` element in `src/QuickReply/QuickReply.csproj`, and the
  corresponding `vX.Y.Z` tag on the GitHub release. The version check
  reads `Assembly.GetName().Version` and compares it to the release tag,
  so the two must agree or running installs will either miss updates or
  nag forever.

## Documentation style
- The README leads with **why** QuickReply exists (the AutoHotkey-script
  origin and reliability problems in browser-based ticket systems) before
  it lists features or build steps. Preserve that ordering on edits.
- Tone is practical and plainspoken. No marketing fluff, no jokes, no
  mascot language. Examples should be real service-desk scenarios
  (follow-ups, voicemails, reboot requests, vendor cases).
- Do not use em dashes (`—`) or en dashes (`–`) in the README. Restructure
  sentences with colons, commas, or periods instead.
- Build, run, and publish commands in the README must match what actually
  works against the current `QuickReply.csproj`. Update them when project
  files change.
