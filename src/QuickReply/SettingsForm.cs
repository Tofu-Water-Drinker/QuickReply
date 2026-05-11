using System.Drawing.Drawing2D;
using QuickReply.Models;

namespace QuickReply;

/// <summary>
/// In-app GUI for every field in appsettings.json. Users no longer need to
/// open the JSON file to change hotkey, paste delays, randomization, the
/// signature code, etc. Save validates the inputs, returns the new
/// <see cref="AppSettings"/> via <see cref="PendingSettings"/>, and lets
/// <see cref="TrayApplicationContext"/> handle live re-application
/// (specifically the hotkey, which can fail and needs rollback).
/// </summary>
public class SettingsForm : Form
{
    private readonly SettingsService _settingsService;

    // Hotkey
    private TextBox _hotkeyInput = null!;
    private CardPanel _hotkeyCard = null!;
    private Label _hotkeyHint = null!;

    // Paste behavior
    private CheckBox _autoPasteCheckbox = null!;
    private CheckBox _restoreClipboardCheckbox = null!;
    private TextBox _pasteDelayInput = null!;
    private CardPanel _pasteDelayCard = null!;
    private TextBox _clipboardDelayInput = null!;
    private CardPanel _clipboardDelayCard = null!;

    // Snippets
    private CheckBox _randomizeCheckbox = null!;
    private TextBox _signatureCodeInput = null!;
    private CardPanel _signatureCodeCard = null!;

    // Updates
    private CheckBox _updateCheckCheckbox = null!;

    // Privacy
    private CheckBox _safeSignatureCheckbox = null!;
    private ActionButton _openDataFolderButton = null!;
    private Label _dataLocationLabel = null!;

    // Footer
    private ActionButton _saveButton = null!;
    private ActionButton _cancelButton = null!;
    private ActionButton _resetButton = null!;
    private Label _statusLabel = null!;

    /// <summary>
    /// Populated on Save. Null if the user cancelled.
    /// </summary>
    public AppSettings? PendingSettings { get; private set; }

    public SettingsForm(SettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeUi();
        LoadFromCurrent();
    }

    private void InitializeUi()
    {
        SuspendLayout();

        Text = "QuickReply - Settings";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        KeyPreview = true;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.75f);
        ClientSize = new Size(640, 880);
        DoubleBuffered = true;

        const int pad = 24;
        var inner = ClientSize.Width - pad * 2;

        // ── Header ────────────────────────────────────────────────────────
        var titleLabel = new Label
        {
            Text = "Settings",
            Font = Theme.H1(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 20)
        };
        var subtitleLabel = new Label
        {
            Text = "Configure how QuickReply behaves. Changes save when you click Save.",
            Font = Theme.Subtitle(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 50)
        };

        // ── Hotkey ────────────────────────────────────────────────────────
        var hotkeyHeader = MakeSectionHeader("HOTKEY", new Point(pad, 100));

        _hotkeyCard = new CardPanel
        {
            Location = new Point(pad, 122),
            Size = new Size(inner, 46),
            FillColor = Theme.Surface,
            BorderColor = Theme.Border,
            BackColor = Theme.Bg
        };
        _hotkeyInput = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.BodyLg(),
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            PlaceholderText = "Ctrl+Alt+;",
            Location = new Point(14, 13),
            Width = inner - 28
        };
        _hotkeyInput.GotFocus  += (_, _) => { _hotkeyCard.IsFocused = true;  _hotkeyCard.Invalidate(); };
        _hotkeyInput.LostFocus += (_, _) => { _hotkeyCard.IsFocused = false; _hotkeyCard.Invalidate(); };
        _hotkeyCard.Controls.Add(_hotkeyInput);

        _hotkeyHint = new Label
        {
            Text = "Modifiers: Ctrl, Alt, Shift, Win. Keys: letter, digit, punctuation, or F1-F12.\nExamples: Ctrl+Alt+;, Ctrl+Shift+Space, Win+Alt+Q.",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 178)
        };

        // ── Paste behavior ───────────────────────────────────────────────
        var pasteHeader = MakeSectionHeader("PASTE BEHAVIOR", new Point(pad, 228));

        _autoPasteCheckbox = new CheckBox
        {
            Text = "Auto-paste after selecting a snippet",
            AutoSize = true,
            Location = new Point(pad, 252),
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg
        };

        _restoreClipboardCheckbox = new CheckBox
        {
            Text = "Restore the previous clipboard contents after paste",
            AutoSize = true,
            Location = new Point(pad, 282),
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg
        };

        var pasteDelayLabel = new Label
        {
            Text = "Paste delay (ms)",
            Font = Theme.Body(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 320)
        };
        _pasteDelayCard = new CardPanel
        {
            Location = new Point(pad + 180, 314),
            Size = new Size(120, 32),
            FillColor = Theme.Surface,
            BorderColor = Theme.Border,
            BackColor = Theme.Bg
        };
        _pasteDelayInput = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.Body(),
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            Location = new Point(10, 7),
            Width = 100
        };
        AttachNumericFilter(_pasteDelayInput);
        _pasteDelayInput.GotFocus  += (_, _) => { _pasteDelayCard.IsFocused = true;  _pasteDelayCard.Invalidate(); };
        _pasteDelayInput.LostFocus += (_, _) => { _pasteDelayCard.IsFocused = false; _pasteDelayCard.Invalidate(); };
        _pasteDelayCard.Controls.Add(_pasteDelayInput);
        var pasteDelayHint = new Label
        {
            Text = "Pause after focusing target, before Ctrl+V. Raise this for slow apps.",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 352)
        };

        var clipboardDelayLabel = new Label
        {
            Text = "Clipboard restore delay (ms)",
            Font = Theme.Body(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 386)
        };
        _clipboardDelayCard = new CardPanel
        {
            Location = new Point(pad + 180, 380),
            Size = new Size(120, 32),
            FillColor = Theme.Surface,
            BorderColor = Theme.Border,
            BackColor = Theme.Bg
        };
        _clipboardDelayInput = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.Body(),
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            Location = new Point(10, 7),
            Width = 100
        };
        AttachNumericFilter(_clipboardDelayInput);
        _clipboardDelayInput.GotFocus  += (_, _) => { _clipboardDelayCard.IsFocused = true;  _clipboardDelayCard.Invalidate(); };
        _clipboardDelayInput.LostFocus += (_, _) => { _clipboardDelayCard.IsFocused = false; _clipboardDelayCard.Invalidate(); };
        _clipboardDelayCard.Controls.Add(_clipboardDelayInput);
        var clipboardDelayHint = new Label
        {
            Text = "How long to wait before restoring the previous clipboard. Raise for picky apps.",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 418)
        };

        // ── Snippets ──────────────────────────────────────────────────────
        var snippetsHeader = MakeSectionHeader("SNIPPETS", new Point(pad, 458));

        _randomizeCheckbox = new CheckBox
        {
            Text = "Pick a random variant when a code has multiple replies",
            AutoSize = true,
            Location = new Point(pad, 482),
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg
        };

        var signatureCodeLabel = new Label
        {
            Text = "Signature code",
            Font = Theme.Body(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 522)
        };
        _signatureCodeCard = new CardPanel
        {
            Location = new Point(pad + 180, 516),
            Size = new Size(200, 32),
            FillColor = Theme.Surface,
            BorderColor = Theme.Border,
            BackColor = Theme.Bg
        };
        _signatureCodeInput = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.Body(),
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            PlaceholderText = "sig",
            Location = new Point(10, 7),
            Width = 180
        };
        _signatureCodeInput.GotFocus  += (_, _) => { _signatureCodeCard.IsFocused = true;  _signatureCodeCard.Invalidate(); };
        _signatureCodeInput.LostFocus += (_, _) => { _signatureCodeCard.IsFocused = false; _signatureCodeCard.Invalidate(); };
        _signatureCodeCard.Controls.Add(_signatureCodeInput);
        var sigHint = new Label
        {
            Text = "The picker code that pastes your signature as rich text (HTML + plain fallback).",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 554)
        };

        // ── Updates ───────────────────────────────────────────────────────
        var updatesHeader = MakeSectionHeader("UPDATES", new Point(pad, 594));

        _updateCheckCheckbox = new CheckBox
        {
            Text = "Check GitHub for new releases on startup",
            AutoSize = true,
            Location = new Point(pad, 618),
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg
        };

        // ── Privacy & data ───────────────────────────────────────────────
        var privacyHeader = MakeSectionHeader("PRIVACY & DATA", new Point(pad, 660));

        _safeSignatureCheckbox = new CheckBox
        {
            Text = "Safe mode for signature HTML (strip scripts, event handlers, javascript: URLs)",
            AutoSize = true,
            Location = new Point(pad, 684),
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg
        };
        var safeHint = new Label
        {
            Text = "Recommended. Disable only if you author HTML you trust and need a tag the sanitizer removes.",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 714)
        };

        _dataLocationLabel = new Label
        {
            Text = $"Data folder: {PathsService.DataDirectory}{(PathsService.IsPortable ? "  (portable mode)" : "")}",
            Font = Theme.Status(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = false,
            Size = new Size(inner - 150, 18),
            Location = new Point(pad, 742)
        };
        _openDataFolderButton = new ActionButton
        {
            Text = "Open folder",
            Style = ActionButtonStyle.Secondary,
            Size = new Size(120, 28),
            Location = new Point(ClientSize.Width - pad - 120, 738),
            BackColor = Theme.Bg
        };
        _openDataFolderButton.Click += (_, _) => OpenDataFolder();

        // ── Footer ────────────────────────────────────────────────────────
        _statusLabel = new Label
        {
            Location = new Point(pad + 2, 826),
            AutoSize = false,
            Size = new Size(inner - 320, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Theme.Status(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg
        };

        _resetButton = new ActionButton
        {
            Text = "Reset to defaults",
            Style = ActionButtonStyle.Ghost,
            Size = new Size(140, 34),
            Location = new Point(ClientSize.Width - pad - 308, 822),
            BackColor = Theme.Bg
        };
        _resetButton.Click += (_, _) => ResetToDefaults();

        _cancelButton = new ActionButton
        {
            Text = "Cancel",
            Style = ActionButtonStyle.Ghost,
            Size = new Size(78, 34),
            Location = new Point(ClientSize.Width - pad - 164, 822),
            BackColor = Theme.Bg
        };
        _cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        _saveButton = new ActionButton
        {
            Text = "Save",
            Style = ActionButtonStyle.Primary,
            Size = new Size(82, 34),
            Location = new Point(ClientSize.Width - pad - 82, 822),
            BackColor = Theme.Bg
        };
        _saveButton.Click += (_, _) => DoSave();

        Controls.Add(titleLabel);
        Controls.Add(subtitleLabel);

        Controls.Add(hotkeyHeader);
        Controls.Add(_hotkeyCard);
        Controls.Add(_hotkeyHint);

        Controls.Add(pasteHeader);
        Controls.Add(_autoPasteCheckbox);
        Controls.Add(_restoreClipboardCheckbox);
        Controls.Add(pasteDelayLabel);
        Controls.Add(_pasteDelayCard);
        Controls.Add(pasteDelayHint);
        Controls.Add(clipboardDelayLabel);
        Controls.Add(_clipboardDelayCard);
        Controls.Add(clipboardDelayHint);

        Controls.Add(snippetsHeader);
        Controls.Add(_randomizeCheckbox);
        Controls.Add(signatureCodeLabel);
        Controls.Add(_signatureCodeCard);
        Controls.Add(sigHint);

        Controls.Add(updatesHeader);
        Controls.Add(_updateCheckCheckbox);

        Controls.Add(privacyHeader);
        Controls.Add(_safeSignatureCheckbox);
        Controls.Add(safeHint);
        Controls.Add(_dataLocationLabel);
        Controls.Add(_openDataFolderButton);

        Controls.Add(_statusLabel);
        Controls.Add(_resetButton);
        Controls.Add(_cancelButton);
        Controls.Add(_saveButton);

        Paint += DrawChrome;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter && e.Control)
            {
                e.SuppressKeyPress = true;
                DoSave();
            }
        };

        ResumeLayout(false);
        PerformLayout();
    }

    private void LoadFromCurrent() => LoadFrom(_settingsService.Current);

    private void LoadFrom(AppSettings s)
    {
        _hotkeyInput.Text = s.Hotkey;
        _autoPasteCheckbox.Checked = s.AutoPaste;
        _restoreClipboardCheckbox.Checked = s.RestoreClipboardAfterPaste;
        _pasteDelayInput.Text = s.PasteDelayMs.ToString();
        _clipboardDelayInput.Text = s.ClipboardRestoreDelayMs.ToString();
        _randomizeCheckbox.Checked = s.RandomizeResponses;
        _signatureCodeInput.Text = s.SignatureCode;
        _updateCheckCheckbox.Checked = s.CheckForUpdatesOnStartup;
        _safeSignatureCheckbox.Checked = s.SafeSignatureMode;
        _statusLabel.Text = string.Empty;
    }

    private void OpenDataFolder()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = PathsService.DataDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowError("Could not open folder: " + ex.Message);
        }
    }

    private void ResetToDefaults()
    {
        var choice = MessageBox.Show(this,
            "Reset every setting to its default value?\nYour snippets and signature stay untouched.",
            "QuickReply",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (choice != DialogResult.Yes) return;
        LoadFrom(new AppSettings());
        _statusLabel.Text = "Defaults loaded. Click Save to apply, or Cancel to abandon.";
        _statusLabel.ForeColor = Theme.AccentSoft;
    }

    private void DoSave()
    {
        var hotkey = _hotkeyInput.Text.Trim();
        if (!IsHotkeyShapeValid(hotkey))
        {
            ShowError("Hotkey must include at least one modifier (Ctrl, Alt, Shift, Win) and a key.");
            _hotkeyInput.Focus();
            return;
        }

        if (!int.TryParse(_pasteDelayInput.Text, out var pasteDelay) || pasteDelay < 0 || pasteDelay > 10_000)
        {
            ShowError("Paste delay must be a whole number between 0 and 10000.");
            _pasteDelayInput.Focus();
            return;
        }

        if (!int.TryParse(_clipboardDelayInput.Text, out var clipboardDelay) || clipboardDelay < 0 || clipboardDelay > 60_000)
        {
            ShowError("Clipboard restore delay must be a whole number between 0 and 60000.");
            _clipboardDelayInput.Focus();
            return;
        }

        var sigCode = _signatureCodeInput.Text.Trim();
        if (string.IsNullOrEmpty(sigCode))
        {
            ShowError("Signature code cannot be empty. Default is \"sig\".");
            _signatureCodeInput.Focus();
            return;
        }

        // Preserve fields the form does not edit (Theme, TutorialShown) so we
        // never clobber them on save.
        var current = _settingsService.Current;
        PendingSettings = new AppSettings
        {
            AutoPaste                  = _autoPasteCheckbox.Checked,
            RestoreClipboardAfterPaste = _restoreClipboardCheckbox.Checked,
            ClipboardRestoreDelayMs    = clipboardDelay,
            PasteDelayMs               = pasteDelay,
            Theme                      = current.Theme,
            Hotkey                     = hotkey,
            CheckForUpdatesOnStartup   = _updateCheckCheckbox.Checked,
            RandomizeResponses         = _randomizeCheckbox.Checked,
            SignatureCode              = sigCode,
            TutorialShown              = current.TutorialShown,
            SafeSignatureMode          = _safeSignatureCheckbox.Checked
        };
        DialogResult = DialogResult.OK;
        Close();
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static Label MakeSectionHeader(string text, Point location) => new()
    {
        Text = text,
        Font = Theme.Caps(),
        ForeColor = Theme.TextMuted,
        BackColor = Theme.Bg,
        AutoSize = true,
        Location = location
    };

    private static void AttachNumericFilter(TextBox tb)
    {
        tb.KeyPress += (_, e) =>
        {
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != '\b')
            {
                e.Handled = true;
            }
        };
    }

    private static bool IsHotkeyShapeValid(string hk)
    {
        if (string.IsNullOrWhiteSpace(hk)) return false;
        var parts = hk.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false;
        var hasModifier = false;
        var hasKey      = false;
        foreach (var p in parts)
        {
            var n = p.ToLowerInvariant();
            if (n is "ctrl" or "control" or "alt" or "shift" or "win" or "windows") hasModifier = true;
            else hasKey = true;
        }
        return hasModifier && hasKey;
    }

    private void ShowError(string message)
    {
        _statusLabel.Text = message;
        _statusLabel.ForeColor = Theme.Danger;
    }

    private void DrawChrome(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var pen = new Pen(Theme.Border, 1f))
            g.DrawLine(pen, 24, 84, ClientSize.Width - 24, 84);
        using (var pen = new Pen(Theme.BorderHi, 1f))
            g.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            const int CS_DROPSHADOW = 0x00020000;
            cp.ClassStyle |= CS_DROPSHADOW;
            return cp;
        }
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x84;
        const int HTCLIENT  = 1;
        const int HTCAPTION = 2;
        if (m.Msg == WM_NCHITTEST)
        {
            base.WndProc(ref m);
            if (m.Result.ToInt32() == HTCLIENT)
            {
                var lp = m.LParam.ToInt32();
                var pt = PointToClient(new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF)));
                if (pt.Y < 84) m.Result = (IntPtr)HTCAPTION;
            }
            return;
        }
        base.WndProc(ref m);
    }
}
