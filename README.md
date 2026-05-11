# QuickReply

A small Windows tray utility for service desk and support workflows. Press a hotkey, type a short code like `fu`, and paste a clean, consistent reply into your ticket.

## Why QuickReply exists

QuickReply started as a simple AutoHotkey script for common service desk responses. The idea was practical: type short triggers like `;fu`, `;vm`, `;close`, `;rbt` and have them expand into pre-written ticket replies, so techs would not have to retype "Following up on this ticket..." for the hundredth time that week.

AutoHotkey was great for prototyping. The problem is that real ticket systems are not always plain text. Browser-based ticket UIs, rich text editors, and SLA portals do not always play nicely with hotstrings. Fields drop characters, the script stops mid-replacement, paste behavior fights the editor, or the trigger fires inside something like a code block where you do not want it. Clipboard-based replacement helped, but it always felt like duct tape. Dialog-based AHK pickers were better, but rough around the edges and still hit reliability limits in browser ticket UIs.

QuickReply is the standalone version of that idea. It is a native Windows tray app that opens from a global hotkey, shows you the matching reply, and either pastes it directly or hands you a clean copy of the text for manual paste. No hotstrings to misfire. No fragile browser injection. Open, pick, paste.

It is built for service desk and tier 1/2 support workflows where the same thirty or so sentences make up most of your written communication: follow-ups, voicemail notes, reboot requests, vendor case updates, escalation summaries, incident updates. The kind of writing that needs to be consistent and fast, not creative.

## What you get

Out of the box, QuickReply ships with the kinds of snippets a service desk tech actually uses every day. A few examples:

| Code | What it says, roughly |
| --- | --- |
| `fu` | "Following up on this ticket. Are you still experiencing the issue?" |
| `vm` | "I left you a voicemail and will follow up again if I do not hear back." |
| `close` | "I am marking this ticket resolved for now. Reply here if it returns." |
| `rbt` | "Please reboot the computer when you have a chance, then let me know." |
| `ts` | Multi-line troubleshooting template (performed / result / next step) |
| `esc` | Escalation summary template (issue / impact / what we need next) |
| `vendorcase` | "We opened a case with the vendor and are waiting on their response." |

The full list lives in `snippets.json`. Edit, add, rename, and reload without restarting the app.

## Features

* Global hotkey snippet picker (default **Ctrl + Alt + ;**, configurable)
* User-editable `snippets.json`
* Live preview of the reply before you paste
* **Paste** and **Copy Only** modes for picky ticket fields
* Dynamic date and time tokens (e.g. `{{date:yyyy-MM-dd}}`)
* In-app snippet editor, no JSON wrangling required
* Tray menu for reload, settings file, snippets file, exit
* Reload snippets without restarting the app
* Single executable, no installer, zero third-party NuGet packages
* Designed for service desk ticket workflows
* .NET 8, WinForms, Windows 10/11

## How it works

1. Press **Ctrl + Alt + ;** anywhere in Windows.
2. The picker opens centered on your active screen, focused on the code input.
3. Type a code like `fu`, or click one of the quick-pick chips.
4. The matching reply previews live.
5. Press **Enter** (or click **Paste**) to paste it into the window you came from.
6. Or click **Copy Only** to drop the snippet on your clipboard for a manual Ctrl+V.

QuickReply remembers the window you were focused on before opening the picker, restores that focus on paste, and (optionally) puts your previous clipboard contents back when it is done.

### About Copy Only

Some web-based ticket systems, rich text editors, and SLA portals fight programmatic paste. They strip the keystroke, double-paste, or drop the input entirely. **Copy Only** sidesteps this: it just puts the snippet on your clipboard and gets out of the way. Then you paste with Ctrl+V yourself, and the ticket field receives a normal paste event with no surprises. If a particular field gives you trouble, Copy Only is the reliable fallback.

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

* Another app may already own `Ctrl+Alt+;`. Common culprits: Visual Studio, IDE plugins, other text expanders. Check for a balloon tip at startup, then pick a different combo in `appsettings.json` and restart.
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
