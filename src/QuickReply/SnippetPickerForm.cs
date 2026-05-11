namespace QuickReply;

public class SnippetPickerForm : Form
{
    private static readonly (string Code, string Label)[] QuickPicks =
    {
        ("fu",     "Follow up"),
        ("vm",     "Voicemail"),
        ("close",  "Close ticket"),
        ("rbt",    "Reboot request"),
        ("ty",     "Thanks"),
        ("dig",    "Digging in"),
        ("wait",   "Need to verify"),
        ("ts",     "Troubleshooting log"),
        ("note",   "Ticket note"),
        ("esc",    "Escalation"),
        ("heads",  "Heads up"),
        ("impact", "Impact update"),
    };

    private readonly SnippetService _snippets;
    private readonly PasteService _paste;
    private readonly SettingsService _settings;
    private readonly SignatureService _signatures;

    private TextBox _codeInput = null!;
    private TextBox _previewBox = null!;
    private CardPanel _codeCard = null!;
    private CardPanel _previewCard = null!;
    private Label _previewHint = null!;
    private Label _matchLabel = null!;
    private Label _statusLabel = null!;
    private FlowLayoutPanel _quickPicksPanel = null!;
    private ActionButton _pasteButton = null!;
    private ActionButton _copyButton = null!;
    private ActionButton _cancelButton = null!;

    public IntPtr PreviousWindow { get; set; } = IntPtr.Zero;

    public SnippetPickerForm(
        SnippetService snippets,
        PasteService paste,
        SettingsService settings,
        SignatureService signatures)
    {
        _snippets = snippets;
        _paste = paste;
        _settings = settings;
        _signatures = signatures;

        InitializeUi();

        _snippets.Reloaded += (_, _) =>
        {
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(UpdatePreview));
            }
        };
    }

    public void ShowPicker()
    {
        _codeInput.Clear();
        _previewBox.Clear();
        _previewHint.Text = "Type a snippet code or click a quick pick below\nto preview the reply text.";
        _previewHint.ForeColor = Theme.TextDim;
        _previewHint.Visible = true;
        _statusLabel.Text = $"{_snippets.Snippets.Count} snippets loaded  ·  Esc to close";
        _statusLabel.ForeColor = Theme.TextMuted;
        _matchLabel.Text = string.Empty;
        _pasteButton.Enabled = false;
        _copyButton.Enabled = false;

        CenterOnActiveScreen();

        if (!Visible) Show();
        TopMost = true;
        BringToFront();
        Activate();
        _codeInput.Focus();
    }

    private void CenterOnActiveScreen()
    {
        var screen = Screen.FromPoint(Cursor.Position) ?? Screen.PrimaryScreen!;
        var bounds = screen.WorkingArea;
        Location = new Point(
            bounds.X + (bounds.Width - Width) / 2,
            bounds.Y + (bounds.Height - Height) / 2);
    }

    private void InitializeUi()
    {
        SuspendLayout();

        Text = "QuickReply - Service Desk Snippets";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.75f);
        ClientSize = new Size(680, 670);
        DoubleBuffered = true;

        const int pad = 24;
        var inner = ClientSize.Width - pad * 2;

        // ── Header ────────────────────────────────────────────────────────
        var titleLabel = new Label
        {
            Text = "QuickReply",
            Font = Theme.H1(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 20)
        };
        var subtitleLabel = new Label
        {
            Text = "Service desk snippet picker",
            Font = Theme.Subtitle(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 50)
        };
        var hotkeyPill = new HotkeyPill
        {
            Text = "Ctrl + Alt + ;",
            Size = new Size(112, 26),
            Location = new Point(ClientSize.Width - pad - 112, 28),
            BackColor = Theme.Bg
        };

        // ── Code section ──────────────────────────────────────────────────
        var codeLabel = new Label
        {
            Text = "SNIPPET CODE",
            Font = Theme.Caps(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 96)
        };

        _codeCard = new CardPanel
        {
            Location = new Point(pad, 116),
            Size = new Size(inner, 46),
            FillColor = Theme.Surface,
            BorderColor = Theme.Border,
            BackColor = Theme.Bg
        };

        var chevron = new Label
        {
            Text = "›",
            Font = Theme.Chevron(),
            ForeColor = Theme.AccentHi,
            BackColor = Theme.Surface,
            AutoSize = false,
            Size = new Size(28, _codeCard.Height),
            Location = new Point(6, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _codeInput = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.BodyLg(),
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            PlaceholderText = "Type a snippet code...",
            Location = new Point(40, 13),
            Width = inner - 56
        };
        _codeInput.TextChanged += (_, _) => UpdatePreview();
        _codeInput.KeyDown += CodeInput_KeyDown;
        _codeInput.GotFocus += (_, _) => { _codeCard.IsFocused = true;  _codeCard.Invalidate(); };
        _codeInput.LostFocus += (_, _) => { _codeCard.IsFocused = false; _codeCard.Invalidate(); };

        _codeCard.Controls.Add(chevron);
        _codeCard.Controls.Add(_codeInput);

        _matchLabel = new Label
        {
            Location = new Point(pad + 2, 168),
            AutoSize = true,
            Text = string.Empty,
            Font = Theme.Status(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg
        };

        // ── Preview section ───────────────────────────────────────────────
        var previewLabel = new Label
        {
            Text = "PREVIEW",
            Font = Theme.Caps(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 200)
        };

        _previewCard = new CardPanel
        {
            Location = new Point(pad, 220),
            Size = new Size(inner, 158),
            FillColor = Theme.BgRaised,
            BorderColor = Theme.Border,
            BackColor = Theme.Bg
        };

        _previewBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.None,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.BgRaised,
            ForeColor = Theme.Text,
            Font = Theme.Preview(),
            WordWrap = true,
            TabStop = false,
            Location = new Point(16, 14),
            Size = new Size(inner - 32, 130)
        };
        _previewCard.Controls.Add(_previewBox);

        _previewHint = new Label
        {
            Text = "Type a snippet code or click a quick pick below\nto preview the reply text.",
            Font = Theme.Preview(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.BgRaised,
            AutoSize = false,
            Size = new Size(inner - 32, 130),
            Location = new Point(16, 14),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _previewCard.Controls.Add(_previewHint);
        _previewHint.BringToFront();

        // ── Quick picks section ───────────────────────────────────────────
        var quickPicksLabel = new Label
        {
            Text = "QUICK PICKS",
            Font = Theme.Caps(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 396)
        };
        var addSnippetButton = new ActionButton
        {
            Text = "+  New snippet",
            Style = ActionButtonStyle.Ghost,
            Size = new Size(130, 26),
            Location = new Point(ClientSize.Width - pad - 130, 388),
            BackColor = Theme.Bg
        };
        addSnippetButton.Click += (_, _) => OpenAddSnippet(prefillCode: null);

        _quickPicksPanel = new FlowLayoutPanel
        {
            Location = new Point(pad, 420),
            Size = new Size(inner, 176),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = false,
            BackColor = Theme.Bg
        };

        var chipWidth = (inner - 24) / 3; // 3 columns, 2 gaps of 6 + slack
        foreach (var (code, label) in QuickPicks)
        {
            var chip = new ChipButton
            {
                Code = code,
                Label = label,
                Size = new Size(chipWidth, 36),
                Margin = new Padding(0, 0, 6, 6),
                BackColor = Theme.Bg
            };
            var capturedCode = code;
            chip.Click += (_, _) =>
            {
                _codeInput.Text = capturedCode;
                _codeInput.SelectionStart = _codeInput.Text.Length;
                UpdatePreview();
                _codeInput.Focus();
            };
            _quickPicksPanel.Controls.Add(chip);
        }

        // ── Footer ────────────────────────────────────────────────────────
        _statusLabel = new Label
        {
            Location = new Point(pad + 2, 614),
            AutoSize = false,
            Size = new Size(inner - 300, 28),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Theme.Status(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg
        };

        _cancelButton = new ActionButton
        {
            Text = "Cancel",
            Style = ActionButtonStyle.Ghost,
            Size = new Size(78, 34),
            Location = new Point(ClientSize.Width - pad - 280, 612),
            BackColor = Theme.Bg
        };
        _cancelButton.Click += (_, _) => Hide();

        _copyButton = new ActionButton
        {
            Text = "Copy Only",
            Style = ActionButtonStyle.Secondary,
            Size = new Size(96, 34),
            Location = new Point(ClientSize.Width - pad - 192, 612),
            BackColor = Theme.Bg
        };
        _copyButton.Click += (_, _) => DoCopyOnly();

        _pasteButton = new ActionButton
        {
            Text = "Paste",
            Style = ActionButtonStyle.Primary,
            Size = new Size(86, 34),
            Location = new Point(ClientSize.Width - pad - 86, 612),
            BackColor = Theme.Bg
        };
        _pasteButton.Click += (_, _) => DoPaste();

        Controls.Add(titleLabel);
        Controls.Add(subtitleLabel);
        Controls.Add(hotkeyPill);
        Controls.Add(codeLabel);
        Controls.Add(_codeCard);
        Controls.Add(_matchLabel);
        Controls.Add(previewLabel);
        Controls.Add(_previewCard);
        Controls.Add(quickPicksLabel);
        Controls.Add(addSnippetButton);
        Controls.Add(_quickPicksPanel);
        Controls.Add(_statusLabel);
        Controls.Add(_cancelButton);
        Controls.Add(_copyButton);
        Controls.Add(_pasteButton);

        Paint += DrawChrome;
        KeyDown += SnippetPickerForm_KeyDown;
        Deactivate += (_, _) =>
        {
            if (Visible) Hide();
        };

        ResumeLayout(false);
        PerformLayout();
    }

    private void DrawChrome(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Subtle divider under the header
        using (var pen = new Pen(Theme.Border, 1f))
        {
            g.DrawLine(pen, 24, 84, ClientSize.Width - 24, 84);
        }

        // 1px outer border so the borderless form has a defined edge
        using (var pen = new Pen(Theme.BorderHi, 1f))
        {
            g.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }
    }

    private void CodeInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            DoPaste();
        }
        else if (e.KeyCode == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            Hide();
        }
    }

    private void SnippetPickerForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            Hide();
        }
    }

    private void UpdatePreview()
    {
        var code = _codeInput.Text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            _previewBox.Text = string.Empty;
            _previewHint.Text = "Type a snippet code or click a quick pick below\nto preview the reply text.";
            _previewHint.ForeColor = Theme.TextDim;
            _previewHint.Visible = true;
            _matchLabel.Text = string.Empty;
            _pasteButton.Enabled = false;
            _copyButton.Enabled = false;
            return;
        }

        // Signature code wins over a same-named snippet so the user always gets
        // rich paste for their signature even if they happen to define "sig"
        // as a regular snippet.
        if (IsSignatureCode(code))
        {
            _previewBox.Text = _signatures.GetPlainText();
            _previewHint.Visible = false;
            _matchLabel.Text = $"●  Match: {code}  (signature, rich paste)";
            _matchLabel.ForeColor = Theme.Success;
            _pasteButton.Enabled = true;
            _copyButton.Enabled = true;
            return;
        }

        var randomize = _settings.Current.RandomizeResponses;
        if (_snippets.TryResolve(code, randomize, out var text))
        {
            _previewBox.Text = text;
            _previewHint.Visible = false;
            _matchLabel.Text = $"●  Match: {code}{DescribeEntry(code, randomize)}";
            _matchLabel.ForeColor = Theme.Success;
            _pasteButton.Enabled = true;
            _copyButton.Enabled = true;
        }
        else
        {
            _previewBox.Text = string.Empty;
            _previewHint.Text = $"No snippet found for \"{code}\"\nClick  +  New snippet  to create one.";
            _previewHint.ForeColor = Theme.Danger;
            _previewHint.Visible = true;
            _matchLabel.Text = $"●  No snippet found for \"{code}\"";
            _matchLabel.ForeColor = Theme.Danger;
            _pasteButton.Enabled = false;
            _copyButton.Enabled = false;
        }
    }

    private bool IsSignatureCode(string code)
    {
        var sig = _settings.Current.SignatureCode;
        return !string.IsNullOrWhiteSpace(sig)
            && string.Equals(code, sig.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns "  (alias -> rbt)" / "  (1 of 8 variants)" / "" depending on the
    /// shape of the matched entry. Pure decoration for the match label.
    /// </summary>
    private string DescribeEntry(string code, bool randomize)
    {
        if (!_snippets.Snippets.TryGetValue(code, out var entry)) return string.Empty;
        if (entry.IsAlias) return $"  (alias -> {entry.AliasTarget})";
        if (entry.Variants.Length > 1)
        {
            return randomize
                ? $"  ({entry.Variants.Length} variants, random)"
                : $"  ({entry.Variants.Length} variants, using first)";
        }
        return string.Empty;
    }

    private void DoPaste()
    {
        var code = _codeInput.Text.Trim();

        if (IsSignatureCode(code))
        {
            var html = _signatures.GetHtml();
            var plain = _signatures.GetPlainText();
            var prevSig = PreviousWindow;
            Hide();
            var sigResult = _paste.PasteOrCopyRich(html, plain, prevSig);
            if (!string.IsNullOrEmpty(sigResult.Message))
            {
                ShowStatus(sigResult.Message, sigResult.Pasted ? Theme.Success : Theme.TextMuted);
            }
            return;
        }

        var randomize = _settings.Current.RandomizeResponses;
        if (!_snippets.TryResolve(code, randomize, out var text))
        {
            ShowStatus($"No snippet found for \"{code}\".", Theme.Danger);
            return;
        }

        var prev = PreviousWindow;
        Hide();

        var result = _paste.PasteOrCopy(text, prev);
        if (!string.IsNullOrEmpty(result.Message))
        {
            ShowStatus(result.Message, result.Pasted ? Theme.Success : Theme.TextMuted);
        }
    }

    private void DoCopyOnly()
    {
        var code = _codeInput.Text.Trim();

        if (IsSignatureCode(code))
        {
            var html = _signatures.GetHtml();
            var plain = _signatures.GetPlainText();
            if (ClipboardService.SetRichText(html, plain))
            {
                ShowStatus("Signature copied (rich text).", Theme.Success);
                Hide();
            }
            else
            {
                ShowStatus("Clipboard is unavailable. Try again.", Theme.Danger);
            }
            return;
        }

        var randomize = _settings.Current.RandomizeResponses;
        if (!_snippets.TryResolve(code, randomize, out var text))
        {
            ShowStatus($"No snippet found for \"{code}\".", Theme.Danger);
            return;
        }

        if (ClipboardService.SetText(text))
        {
            ShowStatus("Copied to clipboard.", Theme.Success);
            Hide();
        }
        else
        {
            ShowStatus("Clipboard is unavailable. Try again.", Theme.Danger);
        }
    }

    private void ShowStatus(string message, Color color)
    {
        _statusLabel.Text = message;
        _statusLabel.ForeColor = color;
    }

    /// <summary>
    /// Opens the Add Snippet dialog. If <paramref name="prefillCode"/> is supplied,
    /// the code field is prefilled (useful when the user has typed a non-existent
    /// code and wants to create it).
    /// </summary>
    public void OpenAddSnippet(string? prefillCode)
    {
        Hide();
        using var dialog = new AddSnippetForm(_snippets, prefillCode);
        var result = dialog.ShowDialog();
        if (result == DialogResult.OK && !string.IsNullOrEmpty(dialog.SavedCode))
        {
            // Re-open the picker pre-filled with the new code so the user can see/use it.
            ShowPicker();
            _codeInput.Text = dialog.SavedCode;
            _codeInput.SelectionStart = _codeInput.Text.Length;
            UpdatePreview();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            base.OnFormClosing(e);
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int CS_DROPSHADOW    = 0x00020000;
            cp.ExStyle |= WS_EX_TOOLWINDOW;
            cp.ClassStyle |= CS_DROPSHADOW;
            return cp;
        }
    }
}
