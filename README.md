# QuickReply

A small native Windows tray app for service desk and ticket response snippets. Hit a hotkey, type a code, paste a polished reply. No bloated AutoHotkey scripts, no browser extensions, no subscriptions.

## What it does

QuickReply lives in your system tray. Press **Ctrl + Alt + ;**, type a short code like `fu`, hit **Enter**, and your reply is pasted into whatever app you were working in. Built for tier-1 and tier-2 support workflows where the same 30 sentences make up most of your day.

* Global hotkey, fully configurable
* Borderless dark-themed picker with live preview
* 12 quick-pick chips for your most-used snippets
* Saves and restores your clipboard contents after paste
* Date and time tokens in snippets (e.g. `{{date:yyyy-MM-dd}}`)
* In-app snippet editor; no JSON wrangling required
* Single executable, no installer
* .NET 8, WinForms, zero third-party NuGet packages

## Quick start

Requires the .NET 8 SDK (or newer with the .NET 8 targeting pack).

```bash
git clone https://github.com/Tofu-Water-Drinker/QuickReply.git
cd QuickReply
dotnet build QuickReply.sln -c Release
dotnet run --project src/QuickReply/QuickReply.csproj -c Release
```

Then press **Ctrl + Alt + ;**, type `fu`, and press Enter.

To build a portable single-file executable you can copy to any Windows 10/11 machine:

```bash
dotnet publish src/QuickReply/QuickReply.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

The resulting `publish\QuickReply.exe` is everything you need.

## Using the picker

| Action | How |
| --- | --- |
| Open the picker | Press the global hotkey, or double-click the tray icon |
| Set the code | Type it, or click a quick-pick chip |
| Paste into the previous window | Press **Enter** or click **Paste** |
| Copy without auto-pasting | Click **Copy Only** |
| Dismiss the picker | Press **Esc**, click **Cancel**, or click outside |
| Add a new snippet | Click **+ New snippet**, or use the tray menu |

The picker remembers which window you came from. After paste, focus jumps back to that window and Ctrl+V is sent automatically. If you would rather paste by hand (for picky web apps that reject programmatic paste), use **Copy Only**.

## Editing snippets

The fastest way is the **+ New snippet** button in the picker, or right-click the tray icon and choose **Add Snippet...**. The editor:

* Detects existing codes and switches into edit mode automatically
* Supports multi-line text directly (no escaping `\n`)
* Saves with **Ctrl + Enter**

For bulk edits, open `snippets.json` directly. It lives next to `QuickReply.exe`. The format is a flat code-to-text object:

```json
{
  "fu": "Following up on this ticket. Are you still experiencing the issue?",
  "vm": "I left you a voicemail and will follow up again if I do not hear back.",
  "date": "{{date:yyyy-MM-dd}}"
}
```

Click **Reload Snippets** in the tray menu after editing. No restart required.

### Dynamic tokens

Any `{{date:FORMAT}}` placeholder is replaced with the current local date and time using a standard .NET format string (`yyyy`, `MM`, `dd`, `HH`, `h`, `mm`, `tt`, and so on). Tokens are expanded at the moment the snippet is used, not when the file is loaded, so timestamps are always current.

## Configuration

`appsettings.json` is created next to `QuickReply.exe` on first launch:

```json
{
  "AutoPaste": true,
  "RestoreClipboardAfterPaste": true,
  "ClipboardRestoreDelayMs": 2500,
  "PasteDelayMs": 150,
  "Theme": "dark",
  "Hotkey": "Ctrl+Alt+;"
}
```

| Setting | Purpose |
| --- | --- |
| `AutoPaste` | If `false`, the Paste button copies only and does not send Ctrl+V |
| `RestoreClipboardAfterPaste` | Saves whatever was on the clipboard before paste, restores it after |
| `ClipboardRestoreDelayMs` | How long to wait before restoring the previous clipboard. Raise this for slow apps |
| `PasteDelayMs` | Pause after focusing the target window, before sending Ctrl+V |
| `Theme` | `dark` (default) or anything else for system default |
| `Hotkey` | Modifiers and key joined with `+`. See below |

### Hotkey format

Modifiers: `Ctrl`, `Alt`, `Shift`, `Win`. Keys: any letter, digit, common punctuation, or `F1` through `F12`, `Space`, `Tab`, `Enter`, `Esc`, `Backspace`.

Examples: `Ctrl+Alt+;`, `Ctrl+Shift+Space`, `Win+Alt+Q`, `Ctrl+F12`.

Settings are read once at startup. After editing the file, right-click the tray icon, choose **Exit**, then launch again.

## Run on Windows startup

1. Press **Win + R**, type `shell:startup`, press Enter.
2. Right-click in that folder, choose **New** then **Shortcut**.
3. Point the shortcut at `QuickReply.exe`.
4. Click **Finish**.

QuickReply will start minimized to the tray on sign-in.

## A note on elevated apps

Windows blocks input from non-elevated processes into elevated windows (UIPI). If you paste into an app running **as administrator** while QuickReply is not, the paste will silently fail. **Copy Only** still works in that case: the snippet lands on the clipboard and you can press Ctrl+V yourself.

If you really need paste to work for elevated targets, run QuickReply itself as administrator.

## Troubleshooting

**The hotkey does not open the picker**

* Another app may already own `Ctrl+Alt+;`. Common culprits: Visual Studio, IDE plugins, other text expanders. Check for a balloon tip at startup; pick a different combo in `appsettings.json` and restart.
* Make sure only one QuickReply is running. It enforces single instance, but a stale process might still hold the hotkey if killed forcefully. Check Task Manager.
* The tray icon always works as a fallback: double-click it to open the picker.

**Paste does not work**

* The target app may be elevated. See the section above. Use **Copy Only** and press Ctrl+V yourself.
* Some web apps reject programmatic paste. Use **Copy Only**.
* Try increasing `PasteDelayMs` to 250 or 400 in `appsettings.json`.
* Some apps need the clipboard to stay put longer. Increase `ClipboardRestoreDelayMs` to 5000 or higher.

**Snippet file is malformed**

* QuickReply shows a warning with the parse error and keeps the previously loaded snippets in memory, so you are not locked out. Fix the JSON (a missing comma is usually the culprit) and click **Reload Snippets**.
* To start over, delete `snippets.json` and click **Reload Snippets**. QuickReply will recreate the defaults.

**Target app eats pasted text**

* Use **Copy Only** and paste manually. This is the reliable fallback.
* Chromium-based ticket systems sometimes intercept paste events when focus has not fully settled. Bumping `PasteDelayMs` helps.

## Project layout

```
QuickReply.sln
src/QuickReply/
  Program.cs
  TrayApplicationContext.cs
  HotkeyManager.cs
  SnippetService.cs
  SettingsService.cs
  ClipboardService.cs
  PasteService.cs
  SnippetPickerForm.cs
  AddSnippetForm.cs
  Theme.cs
  Models/AppSettings.cs
  app.manifest
README.md
ARCHITECTURE.md
```

`snippets.json` and `appsettings.json` are created next to the executable on first launch.

See [ARCHITECTURE.md](ARCHITECTURE.md) for a deeper walkthrough of the codebase.
