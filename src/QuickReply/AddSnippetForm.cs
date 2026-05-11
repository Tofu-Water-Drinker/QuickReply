using System.Drawing.Drawing2D;

namespace QuickReply;

public class AddSnippetForm : Form
{
    private readonly SnippetService _snippets;

    private TextBox _codeInput = null!;
    private TextBox _textInput = null!;
    private CardPanel _codeCard = null!;
    private CardPanel _textCard = null!;
    private ActionButton _saveButton = null!;
    private ActionButton _cancelButton = null!;
    private Label _statusLabel = null!;
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;

    /// <summary>
    /// The code that was saved, if any. Null if the dialog was cancelled.
    /// </summary>
    public string? SavedCode { get; private set; }

    public AddSnippetForm(SnippetService snippets, string? prefillCode = null)
    {
        _snippets = snippets;
        InitializeUi();

        if (!string.IsNullOrWhiteSpace(prefillCode))
        {
            _codeInput.Text = prefillCode;
            _codeInput.SelectionStart = _codeInput.Text.Length;
            UpdateStatus();
        }
    }

    private void InitializeUi()
    {
        SuspendLayout();

        Text = "QuickReply - Add Snippet";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.75f);
        ClientSize = new Size(560, 488);
        DoubleBuffered = true;

        const int pad = 24;
        var inner = ClientSize.Width - pad * 2;

        // ── Header ────────────────────────────────────────────────────────
        _titleLabel = new Label
        {
            Text = "Add Snippet",
            Font = Theme.H1(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 20)
        };
        _subtitleLabel = new Label
        {
            Text = "Create a new shortcut code and reply text",
            Font = Theme.Subtitle(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 50)
        };

        // ── Code ──────────────────────────────────────────────────────────
        var codeLabel = new Label
        {
            Text = "CODE",
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
        _codeInput = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.BodyLg(),
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            PlaceholderText = "e.g. fu",
            Location = new Point(14, 13),
            Width = inner - 28
        };
        _codeInput.GotFocus  += (_, _) => { _codeCard.IsFocused = true;  _codeCard.Invalidate(); };
        _codeInput.LostFocus += (_, _) => { _codeCard.IsFocused = false; _codeCard.Invalidate(); };
        _codeInput.TextChanged += (_, _) => UpdateStatus();
        _codeInput.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _textInput.Focus();
            }
        };
        _codeCard.Controls.Add(_codeInput);

        // ── Reply text ────────────────────────────────────────────────────
        var textLabel = new Label
        {
            Text = "REPLY TEXT",
            Font = Theme.Caps(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 178)
        };
        _textCard = new CardPanel
        {
            Location = new Point(pad, 198),
            Size = new Size(inner, 188),
            FillColor = Theme.BgRaised,
            BorderColor = Theme.Border,
            BackColor = Theme.Bg
        };
        _textInput = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.BgRaised,
            ForeColor = Theme.Text,
            Font = Theme.Preview(),
            WordWrap = true,
            PlaceholderText = "Type the reply text...",
            AcceptsReturn = true,
            AcceptsTab = false,
            TabStop = true,
            Location = new Point(14, 12),
            Size = new Size(inner - 28, 164)
        };
        _textInput.GotFocus  += (_, _) => { _textCard.IsFocused = true;  _textCard.Invalidate(); };
        _textInput.LostFocus += (_, _) => { _textCard.IsFocused = false; _textCard.Invalidate(); };
        _textCard.Controls.Add(_textInput);

        // ── Footer ────────────────────────────────────────────────────────
        var tipLabel = new Label
        {
            Text = "Tip: {{date:yyyy-MM-dd}} inserts today's date  ·  Ctrl+Enter to save",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 402)
        };

        _statusLabel = new Label
        {
            Location = new Point(pad + 2, 430),
            AutoSize = false,
            Size = new Size(inner - 200, 28),
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
            Location = new Point(ClientSize.Width - pad - 174, 430),
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
            Size = new Size(86, 34),
            Location = new Point(ClientSize.Width - pad - 86, 430),
            BackColor = Theme.Bg
        };
        _saveButton.Click += (_, _) => DoSave();

        Controls.Add(_titleLabel);
        Controls.Add(_subtitleLabel);
        Controls.Add(codeLabel);
        Controls.Add(_codeCard);
        Controls.Add(textLabel);
        Controls.Add(_textCard);
        Controls.Add(tipLabel);
        Controls.Add(_statusLabel);
        Controls.Add(_cancelButton);
        Controls.Add(_saveButton);

        Paint += DrawChrome;
        KeyDown += Form_KeyDown;
        Load += (_, _) => _codeInput.Focus();

        ResumeLayout(false);
        PerformLayout();
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

    private void Form_KeyDown(object? sender, KeyEventArgs e)
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
    }

    private void UpdateStatus()
    {
        var code = _codeInput.Text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            _statusLabel.Text = string.Empty;
            _titleLabel.Text = "Add Snippet";
            _subtitleLabel.Text = "Create a new shortcut code and reply text";
            return;
        }
        if (_snippets.Contains(code))
        {
            _statusLabel.Text = $"●  \"{code}\" exists – saving will replace it";
            _statusLabel.ForeColor = Theme.AccentSoft;
            _titleLabel.Text = "Edit Snippet";
            _subtitleLabel.Text = $"Update the reply text for \"{code}\"";
        }
        else
        {
            _statusLabel.Text = "●  new shortcut";
            _statusLabel.ForeColor = Theme.Success;
            _titleLabel.Text = "Add Snippet";
            _subtitleLabel.Text = "Create a new shortcut code and reply text";
        }
        Invalidate(); // header text changed
    }

    private void DoSave()
    {
        var code = _codeInput.Text.Trim();
        var text = _textInput.Text;

        if (string.IsNullOrEmpty(code))
        {
            _statusLabel.Text = "Code cannot be empty.";
            _statusLabel.ForeColor = Theme.Danger;
            _codeInput.Focus();
            return;
        }
        if (string.IsNullOrEmpty(text))
        {
            _statusLabel.Text = "Reply text cannot be empty.";
            _statusLabel.ForeColor = Theme.Danger;
            _textInput.Focus();
            return;
        }

        if (_snippets.AddOrUpdate(code, text))
        {
            SavedCode = code;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int CS_DROPSHADOW    = 0x00020000;
            cp.ExStyle    |= WS_EX_TOOLWINDOW;
            cp.ClassStyle |= CS_DROPSHADOW;
            return cp;
        }
    }
}
