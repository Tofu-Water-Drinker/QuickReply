# QuickReply

A small Windows tray utility for service desk and support workflows. Press a hotkey, type a short code like `fu`, and paste a clean, consistent reply into your ticket.

[![Latest release](https://img.shields.io/github/v/release/Tofu-Water-Drinker/QuickReply?label=latest&color=6366f1)](https://github.com/Tofu-Water-Drinker/QuickReply/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Tofu-Water-Drinker/QuickReply/total?color=6366f1)](https://github.com/Tofu-Water-Drinker/QuickReply/releases)
![Windows](https://img.shields.io/badge/Windows-10%2F11-6366f1?logo=windows&logoColor=white)
![.NET 8](https://img.shields.io/badge/.NET-8-6366f1?logo=dotnet&logoColor=white)

> **v1.3.0** added a first-launch tutorial and an in-app Settings dialog, so you never need to open a JSON file to change behavior. [Grab the installer.](https://github.com/Tofu-Water-Drinker/QuickReply/releases/latest/download/QuickReplySetup.exe)

## Why QuickReply exists

QuickReply started as a simple AutoHotkey script for common service desk responses. The idea was practical: type short triggers like `;fu`, `;vm`, `;close`, `;rbt` and have them expand into pre-written ticket replies, so techs would not have to retype "Following up on this ticket..." for the hundredth time that week.

AutoHotkey was great for prototyping. The problem is that real ticket systems are not always plain text. Browser-based ticket UIs, rich text editors, and SLA portals do not always play nicely with hotstrings. Fields drop characters, the script stops mid-replacement, paste behavior fights the editor, or the trigger fires inside something like a code block where you do not want it. Clipboard-based replacement helped, but it always felt like duct tape. Dialog-based AHK pickers were better, but rough around the edges and still hit reliability limits in browser ticket UIs.

QuickReply is the standalone version of that idea. It is a native Windows tray app that opens from a global hotkey, shows you the matching reply, and either pastes it directly or hands you a clean copy of the text for manual paste. No hotstrings to misfire. No fragile browser injection. Open, pick, paste.

It is built for service desk and tier 1/2 support workflows where the same thirty or so sentences make up most of your written communication: follow-ups, voicemail notes, reboot requests, vendor case updates, escalation summaries, incident updates. The kind of writing that needs to be consistent and fast, not creative.

## What's new

### v1.3.0 (current)

* **First-launch tutorial.** A 5-page tour runs automatically the first time you start QuickReply. Covers the hotkey, what ships with the app (snippets, variants, aliases, signature), and how to manage everything from the tray. Skip button on every page; the flag that suppresses replays is `TutorialShown` in `appsettings.json`.
* **Replay the tutorial any time.** New `Show Tutorial...` item in the tray menu.
* **In-app Settings dialog.** New `Settings...` tray menu item opens a GUI for every field in `appsettings.json`: hotkey, paste delays, AutoPaste, randomization, signature code, update check. No more editing the JSON by hand.
* **Live hotkey re-registration.** Change your hotkey in Settings, click Save, and the new combo is live immediately. If the new combo is already taken by another app, QuickReply rolls back to the previous hotkey and tells you.
* **Reset to defaults** button in Settings restores every field to its built-in value (your snippets and signature stay untouched).

### v1.2.x

* **v1.2.3** Hotkey is now a toggle: press once to open the picker, press again to close.
* **v1.2.2** Inner-control focus restore for ConnectWise Manage and other apps where the outer window regaining foreground does not auto-restore keyboard focus to the email field.
* **v1.2.1** Reliable focus restore before paste, via the AttachThreadInput trick and a deferred-paste BeginInvoke.
* **v1.2.0** Signature with rich paste: dedicated `signature.html`, side-by-side HTML editor with live preview, embedded base64 images, three preset templates, and tray menu shortcuts (`Edit Signature...`, `Copy Signature`).

### v1.1.0

* **Reply variants.** Each conversational code ships with eight different ways to say the same thing. The picker chooses one at random so customers on different tickets see different wording instead of the same paragraph copy-pasted ten times.
* **Aliases.** Type `rbt`, `reboot`, or `restart` and get the same reply. About 30 aliases bundled with the defaults so you do not have to remember your own shorthand.
* **Manage Snippets dialog.** A new tray menu item that lists every snippet, filters live, and lets you edit, delete, or add from one place.
* **Randomized responses preference.** A `RandomizeResponses` switch in `appsettings.json` and on the setup wizard's Preferences page.
* **Forward-compatible JSON.** Old single-string entries keep working; new entries can be arrays of variants or `"@target"` aliases.

### v1.0.0

* First public release. Global hotkey snippet picker, dark-themed picker UI, in-app snippet editor, setup wizard with optional Windows startup integration, quiet GitHub update check, Copy Only fallback for picky ticket fields.

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
* **Variants per code**: ship multiple ways to say the same thing under one shortcut, picked at random so replies do not sound copy-pasted
* **Aliases**: type `rbt`, `reboot`, or `restart` and get the same reply, so you do not have to remember your own shorthand
* **Rich-text signature** with HTML editor, live preview, embedded images, and three preset templates. Pastes as HTML in apps that support it, plain text everywhere else.
* Dynamic date and time tokens (e.g. `{{date:yyyy-MM-dd}}`)
* **Manage Snippets** dialog: see, edit, delete, and add snippets from a single list
* In-app **Add Snippet** editor with multi-variant support, no JSON wrangling required
* Tray menu for manage, reload, settings file, snippets file, exit
* Reload snippets without restarting the app
* Quiet GitHub update check on startup, plus an on-demand "Check for Updates..." menu item
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

## Install

### Option 1: Run the setup wizard (recommended for end users)

Download `QuickReplySetup.exe` from the [latest release](https://github.com/Tofu-Water-Drinker/QuickReply/releases/latest) and double-click it. The wizard walks you through:

1. **Welcome.** What you are installing.
2. **Install location.** Defaults to `%LOCALAPPDATA%\Programs\QuickReply` (no admin needed). Browse to a different folder if you prefer.
3. **Hotkey.** Keep the default `Ctrl + Alt + ;` or pick your own combination.
4. **Snippets.** Start with the included service-desk snippets (variants and aliases bundled), start empty, or define your own in a small grid.
5. **Preferences.** Opt in to launching QuickReply with Windows, and choose whether responses should be randomized when a code has multiple variants.
6. **Summary.** Review your choices.
7. **Install.** The wizard downloads the latest `QuickReply.exe` from this repository's releases, writes `appsettings.json` and (if you chose custom) `snippets.json`, and optionally registers QuickReply under `HKCU\...\Run` for startup.

After install, the wizard offers to launch QuickReply right away. Press your hotkey and you are off.

### Option 2: Plain download

If you do not want the wizard, grab `QuickReply.exe` directly from the [latest release](https://github.com/Tofu-Water-Drinker/QuickReply/releases/latest) and drop it anywhere. On first launch it creates `snippets.json` and `appsettings.json` next to itself with the defaults.

### Option 3: Build from source

Requires the .NET 8 SDK (or newer with the .NET 8 targeting pack).

```bash
git clone https://github.com/Tofu-Water-Drinker/QuickReply.git
cd QuickReply
dotnet build QuickReply.sln -c Release
dotnet run --project src/QuickReply/QuickReply.csproj -c Release
```

Then press **Ctrl + Alt + ;**, type `fu`, and press Enter.

To build the portable single-file executables you can ship to other machines:

```bash
# The main app
dotnet publish src/QuickReply/QuickReply.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish

# The setup wizard (downloads the main app from GitHub releases at install time)
dotnet publish src/QuickReplySetup/QuickReplySetup.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish-setup
```

The resulting `publish\QuickReply.exe` and `publish-setup\QuickReplySetup.exe` are everything you need.

## Using the picker

| Action | How |
| --- | --- |
| Open the picker | Press the global hotkey, or double-click the tray icon |
| Set the code | Type it, or click a quick-pick chip |
| Paste into the previous window | Press **Enter** or click **Paste** |
| Copy without auto-pasting | Click **Copy Only** |
| Dismiss the picker | Press **Esc**, click **Cancel**, or click outside |
| Add a new snippet | Click **+ New snippet**, or use the tray menu |

## Tray menu

Right-click the tray icon for the full menu:

| Item | What it does |
| --- | --- |
| Open QuickReply | Opens the picker (same as the hotkey or double-clicking the tray icon) |
| Manage Snippets... | Opens the filterable snippet list with edit, delete, and add |
| Add Snippet... | Opens the focused single-snippet editor |
| Edit Signature... | Opens the HTML signature editor with live preview |
| Copy Signature | Puts your signature on the clipboard (rich HTML + plain text fallback) without opening the picker |
| Reload Snippets | Re-reads `snippets.json` from disk, no restart needed |
| Open Snippets File | Opens `snippets.json` in your default editor |
| Open Settings File | Opens `appsettings.json` in your default editor |
| Settings... | Opens the in-app Settings dialog (hotkey, paste delays, randomization, signature code, update check) |
| Show Tutorial... | Replays the first-launch tutorial |
| Check for Updates... | Hits GitHub and tells you whether there is a newer release |
| Exit | Quits QuickReply and unregisters the global hotkey |

Double-clicking the tray icon also opens the picker.

## Editing snippets

Two in-app paths, plus a JSON file for power users.

### Manage Snippets

Right-click the tray icon and choose **Manage Snippets...**. You get a filterable list of everything you have, showing the code, the type (single, N variants, or alias), and a preview of the first variant. From here:

* **Double-click** a row to edit it.
* Select a row and press **Delete**, or click the **Delete** button, to remove a snippet.
* Click **Add** to create a new one.
* Type into the filter box to narrow the list by code or by reply text.

This is the dialog to use when you want to clean up, rename, or refresh phrases you have been using for a while.

### Add or edit a single snippet

The **+ New snippet** button in the picker (top-right of the Quick Picks section) opens the focused single-snippet editor. So does **Add Snippet...** in the tray menu. The editor:

* Detects existing codes and switches into edit mode automatically
* Supports one or many reply variants, each in its own multi-line text box
* Has **+ Add variant** to add another, and a **Remove** button on each
* Saves with **Ctrl + Enter**

### Reply variants

Variants are different ways to say the same thing under one code. When the picker uses a code that has multiple variants, it picks one at random. This is what stops your customers from seeing the exact same paragraph on every ticket.

For example, `fu` ships with eight variants. Some of them:

```
Following up on this ticket. Are you still experiencing the issue?
Just checking in on this one. Has anything changed since we last spoke?
Wanted to circle back on this ticket. Still seeing the issue, or are things working again?
Touching base on this ticket. Is the issue still happening?
```

Each time you press your hotkey and use `fu`, one of these gets pasted. The match label in the picker shows `Match: fu  (8 variants, random)` so you can tell.

Randomization is on by default and can be disabled in `appsettings.json` (`"RandomizeResponses": false`). With it off, the first variant is always used.

### Aliases

Sometimes you do not remember whether you saved a snippet as `rbt`, `reboot`, or `restart`. Aliases let all three resolve to the same reply. In `snippets.json`, an alias is just a string starting with `@`:

```json
{
  "rbt": [
    "Please reboot the computer when you have a chance.",
    "Could you give the computer a restart when you get a moment?"
  ],
  "reboot": "@rbt",
  "restart": "@rbt",
  "rb": "@rbt"
}
```

The defaults ship with about 30 aliases (`reboot`, `restart`, `voicemail`, `thanks`, `followup`, `checkin`, `vc`, `fyi`, and so on) so you can use whichever shorthand sticks in your head.

Aliases follow each other up to 8 hops, so an alias of an alias works. Loops are detected and ignored.

### Signature

QuickReply ships with a separate signature feature for the styled, image-bearing block you put at the bottom of ticket replies. Unlike snippets (plain text), the signature pastes as **rich HTML** in apps that support it (Outlook, Gmail web, Teams, ServiceNow rich-text fields) and falls back to plain text in apps that do not.

**Storage.** The signature lives in `signature.html` next to `QuickReply.exe`. First launch creates it from the default template; you edit it in place.

**Editing.** Right-click the tray icon and choose **Edit Signature...**. The editor has two panes:

* **HTML editor** on the left. You can write standard HTML with inline `style="..."` attributes for fonts, colors, sizing, links, and tables.
* **Live preview** on the right, using the system's HTML renderer. What you see is what gets pasted.

The toolbar has:

* **Insert image...** Picks a file (PNG, JPG, GIF, BMP) and embeds it as a base64 data URI inside an `<img>` tag, so the signature is self-contained.
* **Templates...** Switches to one of three preset templates: Default (with contact info), Minimal, or With Logo (placeholder block).
* **Reset to default** Wipes your edits and restores the default template.

**Using the signature.** Two paths:

* **From the picker.** Press your hotkey, type `sig`, press Enter. The match label shows `Match: sig  (signature, rich paste)`. The picker remembers the previous window, restores focus, and sends Ctrl+V. The active app receives both HTML and plain text on the clipboard.
* **Copy Signature tray item.** One click. Puts the rich signature on your clipboard. You paste with Ctrl+V yourself. Useful when you want to paste into a window the picker did not capture.

**Changing the code.** If `sig` clashes with one of your own snippets, change `SignatureCode` in `appsettings.json` to anything you prefer (`signature`, `mysig`, `s`).

**Images and size.** Images are embedded as base64, which inflates them by about 33%. For email signatures, a logo under 50 KB is usually fine. The editor flags signatures over ~200 KB so you can decide whether to compress.

### snippets.json format

For bulk edits or version-controlling your library, open `snippets.json` directly. It lives next to `QuickReply.exe`. The format is a flat object where each value is one of:

* A string: single-variant reply
* An array of strings: multiple variants (random selection)
* `"@target"`: alias to another code

```json
{
  "fu": [
    "Following up on this ticket...",
    "Just checking in on this one..."
  ],
  "ty": "Thanks for the update.",
  "thanks": "@ty",
  "date": "{{date:yyyy-MM-dd}}"
}
```

Click **Reload Snippets** in the tray menu after editing. No restart required.

### Dynamic tokens

Any `{{date:FORMAT}}` placeholder is replaced with the current local date and time using a standard .NET format string (`yyyy`, `MM`, `dd`, `HH`, `h`, `mm`, `tt`, and so on). Tokens are expanded at the moment the snippet is used, not when the file is loaded, so timestamps are always current. Tokens are expanded after the variant is selected, so dated snippets always show the time of paste.

## Configuration

`appsettings.json` is created next to `QuickReply.exe` on first launch:

```json
{
  "AutoPaste": true,
  "RestoreClipboardAfterPaste": true,
  "ClipboardRestoreDelayMs": 2500,
  "PasteDelayMs": 150,
  "Theme": "dark",
  "Hotkey": "Ctrl+Alt+;",
  "CheckForUpdatesOnStartup": true,
  "RandomizeResponses": true,
  "SignatureCode": "sig",
  "TutorialShown": false
}
```

Tip: you do not actually need to edit this file. The tray menu's **Settings...** item opens a GUI that covers every option here. Open `appsettings.json` directly only if you want to script changes or sync settings across machines.

| Setting | Purpose |
| --- | --- |
| `AutoPaste` | If `false`, the Paste button copies only and does not send Ctrl+V |
| `RestoreClipboardAfterPaste` | Saves whatever was on the clipboard before paste, restores it after |
| `ClipboardRestoreDelayMs` | How long to wait before restoring the previous clipboard. Raise this for slow apps |
| `PasteDelayMs` | Pause after focusing the target window, before sending Ctrl+V |
| `Theme` | `dark` (default) or anything else for system default |
| `Hotkey` | Modifiers and key joined with `+`. See below |
| `CheckForUpdatesOnStartup` | If `true` (default), checks GitHub for a newer release shortly after launch |
| `RandomizeResponses` | If `true` (default), picks a random variant when a code has multiple replies. If `false`, always uses the first variant. |
| `SignatureCode` | The picker code that triggers a rich-text signature paste. Defaults to `sig`. Change it if you want to define your own snippet at `sig`. |
| `TutorialShown` | Set to `true` once the first-launch tutorial has been seen. Reset to `false` (or use the tray menu's **Show Tutorial...** item) to replay it. |

### Hotkey format

Modifiers: `Ctrl`, `Alt`, `Shift`, `Win`. Keys: any letter, digit, common punctuation, or `F1` through `F12`, `Space`, `Tab`, `Enter`, `Esc`, `Backspace`.

Examples: `Ctrl+Alt+;`, `Ctrl+Shift+Space`, `Win+Alt+Q`, `Ctrl+F12`.

Settings are read once at startup. After editing the file, right-click the tray icon, choose **Exit**, then launch again.

## Updates

QuickReply checks GitHub for a newer release on startup. The check is quiet: nothing happens if you are on the latest version, and a single tray balloon appears if an update is available. Click the balloon to open the GitHub releases page.

You can also trigger a check on demand from the tray menu via **Check for Updates...**. That path always reports the result, including a "you are on the latest version" confirmation.

If you do not want the startup check, set `CheckForUpdatesOnStartup` to `false` in `appsettings.json`. The on-demand menu item still works.

No automatic install. QuickReply will never overwrite itself while running. To upgrade, download the new `QuickReply.exe` from the releases page and replace your existing copy.

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

**Update check fails**

* QuickReply hits `api.github.com`. If you are offline or behind a proxy that blocks it, the startup check silently gives up and the manual menu item reports the error.
* GitHub allows 60 unauthenticated requests per hour per IP. You are very unlikely to hit that, but if you do, wait an hour or set `CheckForUpdatesOnStartup` to `false`.

## Project layout

```
QuickReply.sln
src/QuickReply/                  the tray app
  Program.cs
  TrayApplicationContext.cs
  HotkeyManager.cs
  SnippetService.cs
  SettingsService.cs
  ClipboardService.cs
  PasteService.cs
  SnippetPickerForm.cs
  AddSnippetForm.cs
  SnippetManagerForm.cs
  SignatureService.cs
  SignatureEditorForm.cs
  SettingsForm.cs
  TutorialForm.cs
  Theme.cs
  UpdateService.cs
  FocusHelper.cs
  snippets-defaults.json   (embedded resource: default snippet set)
  Models/AppSettings.cs
  app.manifest
src/QuickReplySetup/             the setup wizard
  Program.cs
  SetupWizardForm.cs
  Installer.cs
  SetupChoices.cs
  app.manifest
README.md
```

`snippets.json` and `appsettings.json` are created next to the executable on first launch.
