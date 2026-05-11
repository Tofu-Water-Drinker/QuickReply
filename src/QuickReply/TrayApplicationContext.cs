using System.Diagnostics;

namespace QuickReply;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly HotkeyManager _hotkeys;
    private readonly SnippetService _snippets;
    private readonly SettingsService _settings;
    private readonly SignatureService _signatures;
    private readonly PasteService _paste;
    private readonly SnippetPickerForm _picker;
    private readonly UpdateService _updates;
    private string? _pendingUpdateUrl;

    public TrayApplicationContext()
    {
        var baseDir = AppContext.BaseDirectory;
        var snippetsPath  = Path.Combine(baseDir, "snippets.json");
        var settingsPath  = Path.Combine(baseDir, "appsettings.json");
        var signaturePath = Path.Combine(baseDir, "signature.html");

        _settings = new SettingsService(settingsPath);
        _settings.LoadOrCreate();

        _snippets = new SnippetService(snippetsPath);
        _snippets.LoadOrCreate();

        _signatures = new SignatureService(signaturePath);
        _signatures.LoadOrCreate();

        _paste = new PasteService(_settings);
        _picker = new SnippetPickerForm(_snippets, _paste, _settings, _signatures);
        _updates = new UpdateService();

        _trayIcon = new NotifyIcon
        {
            Icon = BuildTrayIcon(),
            Visible = true,
            Text = "QuickReply",
            ContextMenuStrip = BuildTrayMenu()
        };
        _trayIcon.DoubleClick += (_, _) => OpenPicker();
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                OpenPicker();
            }
        };
        _trayIcon.BalloonTipClicked += OnUpdateBalloonClicked;

        _hotkeys = new HotkeyManager();
        _hotkeys.HotkeyPressed += (_, _) => OpenPicker();

        if (!_hotkeys.Register(_settings.Current.Hotkey))
        {
            _trayIcon.ShowBalloonTip(
                4000,
                "QuickReply",
                $"Could not register hotkey \"{_settings.Current.Hotkey}\". " +
                "Another app may already own it. Tray icon still works.",
                ToolTipIcon.Warning);
        }
        else
        {
            _trayIcon.ShowBalloonTip(
                2500,
                "QuickReply",
                $"Running. Press {_hotkeys.CurrentHotkeyDisplay} to open the picker.",
                ToolTipIcon.Info);
        }

        Application.ApplicationExit += OnApplicationExit;

        if (_settings.Current.CheckForUpdatesOnStartup)
        {
            _ = AutoCheckUpdatesAsync();
        }
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open QuickReply", null, (_, _) => OpenPicker());
        menu.Items.Add("Manage Snippets...", null, (_, _) => OpenSnippetManager());
        menu.Items.Add("Add Snippet...", null, (_, _) => OpenAddSnippet());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Edit Signature...", null, (_, _) => OpenSignatureEditor());
        menu.Items.Add("Copy Signature", null, (_, _) => CopySignatureNow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Reload Snippets", null, (_, _) => ReloadSnippets());
        menu.Items.Add("Open Snippets File", null, (_, _) => OpenFile(_snippets.FilePath));
        menu.Items.Add("Open Settings File", null, (_, _) => OpenFile(_settings.FilePath));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Check for Updates...", null, (_, _) => CheckForUpdatesNow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());
        return menu;
    }

    private async Task AutoCheckUpdatesAsync()
    {
        try
        {
            // Brief delay so we don't compete with app startup work.
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(true);
            var result = await _updates.CheckAsync().ConfigureAwait(true);
            if (!result.Success || !result.UpdateAvailable) return;

            _pendingUpdateUrl = result.ReleaseUrl;
            _trayIcon.ShowBalloonTip(
                6000,
                "QuickReply update available",
                $"A new version ({result.ReleaseTag}) is on GitHub. " +
                "Click here to open the releases page.",
                ToolTipIcon.Info);
        }
        catch
        {
            // Fire-and-forget: never let a failed update check disturb the user.
        }
    }

    private async void CheckForUpdatesNow()
    {
        _trayIcon.ShowBalloonTip(1500, "QuickReply", "Checking for updates...", ToolTipIcon.None);
        var result = await _updates.CheckAsync().ConfigureAwait(true);

        if (!result.Success)
        {
            MessageBox.Show(
                $"Could not check for updates.\n\n{result.ErrorMessage}\n\n" +
                $"Releases page: {result.ReleaseUrl}",
                "QuickReply",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (result.UpdateAvailable)
        {
            var choice = MessageBox.Show(
                $"A new version of QuickReply is available.\n\n" +
                $"Installed:  v{result.CurrentVersion}\n" +
                $"Available:  {result.ReleaseTag}\n\n" +
                $"Open the GitHub releases page now?",
                "QuickReply update available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            if (choice == DialogResult.Yes && !string.IsNullOrEmpty(result.ReleaseUrl))
            {
                OpenUrl(result.ReleaseUrl);
            }
        }
        else
        {
            MessageBox.Show(
                $"You are running the latest version (v{result.CurrentVersion}).",
                "QuickReply",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void OnUpdateBalloonClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_pendingUpdateUrl))
        {
            OpenUrl(_pendingUpdateUrl);
            _pendingUpdateUrl = null;
        }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not open URL: " + ex.Message, "QuickReply",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OpenPicker()
    {
        // Capture the foreground window BEFORE the picker shows.
        _picker.PreviousWindow = PasteService.CaptureForegroundWindow();
        _picker.ShowPicker();
    }

    private void OpenAddSnippet()
    {
        using var dialog = new AddSnippetForm(_snippets);
        dialog.ShowDialog();
    }

    private void OpenSnippetManager()
    {
        using var dialog = new SnippetManagerForm(_snippets);
        dialog.ShowDialog();
    }

    private void OpenSignatureEditor()
    {
        using var dialog = new SignatureEditorForm(_signatures);
        dialog.ShowDialog();
    }

    private void CopySignatureNow()
    {
        var html  = _signatures.GetHtml();
        var plain = _signatures.GetPlainText();
        if (ClipboardService.SetRichText(html, plain))
        {
            _trayIcon.ShowBalloonTip(1800, "QuickReply",
                "Signature copied (rich text). Paste with Ctrl+V.",
                ToolTipIcon.Info);
        }
        else
        {
            _trayIcon.ShowBalloonTip(2500, "QuickReply",
                "Could not access the clipboard. Try again.",
                ToolTipIcon.Warning);
        }
    }

    private void ReloadSnippets()
    {
        if (_snippets.LoadOrCreate())
        {
            _trayIcon.ShowBalloonTip(
                1800,
                "QuickReply",
                $"Reloaded {_snippets.Snippets.Count} snippets.",
                ToolTipIcon.Info);
        }
    }

    private void OpenFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not open file: " + ex.Message, "QuickReply",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnApplicationExit(object? sender, EventArgs e)
    {
        _hotkeys.UnregisterIfNeeded();
    }

    private void ExitApp()
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try { _hotkeys.Dispose(); } catch { /* ignore */ }
            try { _trayIcon.Visible = false; _trayIcon.Dispose(); } catch { /* ignore */ }
            try { _picker.Dispose(); } catch { /* ignore */ }
            try { _updates.Dispose(); } catch { /* ignore */ }
        }
        base.Dispose(disposing);
    }

    private static Icon BuildTrayIcon()
    {
        // Draw a simple speech-bubble glyph so we don't ship a binary resource.
        const int size = 32;
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var brush = new SolidBrush(Color.FromArgb(0, 120, 215));
            g.FillRoundedRectangle(brush, new Rectangle(2, 4, 28, 20), 6);

            var tail = new[]
            {
                new Point(10, 22),
                new Point(8, 30),
                new Point(16, 22)
            };
            g.FillPolygon(brush, tail);

            using var dotBrush = new SolidBrush(Color.White);
            g.FillEllipse(dotBrush, 9, 12, 4, 4);
            g.FillEllipse(dotBrush, 14, 12, 4, 4);
            g.FillEllipse(dotBrush, 19, 12, 4, 4);
        }
        var hIcon = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        return icon;
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
