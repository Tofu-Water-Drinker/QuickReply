using System.Drawing.Drawing2D;
using System.Net;

namespace QuickReply;

public class SignatureEditorForm : Form
{
    private readonly SignatureService _signatures;

    private TextBox _htmlEditor = null!;
    private CardPanel _editorCard = null!;
    private WebBrowser _preview = null!;
    private CardPanel _previewCard = null!;
    private ActionButton _saveButton = null!;
    private ActionButton _cancelButton = null!;
    private ActionButton _insertImageButton = null!;
    private ActionButton _resetButton = null!;
    private ActionButton _templateButton = null!;
    private Label _statusLabel = null!;
    private bool _isDirty;

    public SignatureEditorForm(SignatureService signatures)
    {
        _signatures = signatures;
        InitializeUi();

        _htmlEditor.Text = _signatures.GetHtml();
        UpdatePreview();
        _isDirty = false;
        UpdateStatus();
    }

    private void InitializeUi()
    {
        SuspendLayout();

        Text = "QuickReply - Signature";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        KeyPreview = true;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.75f);
        ClientSize = new Size(960, 620);
        DoubleBuffered = true;

        const int pad = 24;
        var inner = ClientSize.Width - pad * 2;
        var paneWidth = (inner - 24) / 2;

        // ── Header ────────────────────────────────────────────────────────
        var titleLabel = new Label
        {
            Text = "Signature",
            Font = Theme.H1(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 20)
        };
        var subtitleLabel = new Label
        {
            Text = "Edit the HTML on the left. The right pane previews how it will look when pasted.",
            Font = Theme.Subtitle(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 50)
        };

        // ── Toolbar ───────────────────────────────────────────────────────
        _insertImageButton = new ActionButton
        {
            Text = "Insert image...",
            Style = ActionButtonStyle.Secondary,
            Size = new Size(130, 30),
            Location = new Point(pad, 92),
            BackColor = Theme.Bg
        };
        _insertImageButton.Click += (_, _) => InsertImageFromFile();

        _templateButton = new ActionButton
        {
            Text = "Templates...",
            Style = ActionButtonStyle.Secondary,
            Size = new Size(120, 30),
            Location = new Point(pad + 138, 92),
            BackColor = Theme.Bg
        };
        _templateButton.Click += (_, _) => ShowTemplateMenu();

        _resetButton = new ActionButton
        {
            Text = "Reset to default",
            Style = ActionButtonStyle.Ghost,
            Size = new Size(140, 30),
            Location = new Point(pad + 266, 92),
            BackColor = Theme.Bg
        };
        _resetButton.Click += (_, _) =>
        {
            var choice = MessageBox.Show(this,
                "Replace the current signature with the default template?\n\nThis cannot be undone (unless you cancel without saving).",
                "QuickReply",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (choice == DialogResult.Yes)
            {
                _htmlEditor.Text = SignatureService.DefaultTemplateHtml;
            }
        };

        // ── Editor pane ───────────────────────────────────────────────────
        var editorLabel = new Label
        {
            Text = "HTML",
            Font = Theme.Caps(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 140)
        };
        _editorCard = new CardPanel
        {
            Location = new Point(pad, 160),
            Size = new Size(paneWidth, 360),
            FillColor = Theme.BgRaised,
            BorderColor = Theme.Border,
            BackColor = Theme.Bg
        };
        _htmlEditor = new TextBox
        {
            Multiline = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = false,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.BgRaised,
            ForeColor = Theme.Text,
            Font = new Font("Consolas", 10f),
            Location = new Point(12, 10),
            Size = new Size(_editorCard.Width - 24, _editorCard.Height - 20)
        };
        _htmlEditor.TextChanged += (_, _) =>
        {
            _isDirty = true;
            UpdatePreview();
            UpdateStatus();
        };
        _htmlEditor.GotFocus  += (_, _) => { _editorCard.IsFocused = true;  _editorCard.Invalidate(); };
        _htmlEditor.LostFocus += (_, _) => { _editorCard.IsFocused = false; _editorCard.Invalidate(); };
        _editorCard.Controls.Add(_htmlEditor);

        // ── Preview pane ──────────────────────────────────────────────────
        var previewLabel = new Label
        {
            Text = "PREVIEW",
            Font = Theme.Caps(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + paneWidth + 24, 140)
        };
        _previewCard = new CardPanel
        {
            Location = new Point(pad + paneWidth + 24, 160),
            Size = new Size(paneWidth, 360),
            FillColor = Color.White,
            BorderColor = Theme.Border,
            BackColor = Theme.Bg
        };
        _preview = new WebBrowser
        {
            Location = new Point(4, 4),
            Size = new Size(_previewCard.Width - 8, _previewCard.Height - 8),
            AllowNavigation = false,
            ScriptErrorsSuppressed = true,
            IsWebBrowserContextMenuEnabled = false,
            WebBrowserShortcutsEnabled = false,
            AllowWebBrowserDrop = false
        };
        _previewCard.Controls.Add(_preview);

        // ── Footer ────────────────────────────────────────────────────────
        var tip = new Label
        {
            Text = "Tip: paste with the picker by typing the signature code (default \"sig\"). Inline images are embedded as base64.",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 538)
        };
        _statusLabel = new Label
        {
            Location = new Point(pad + 2, 580),
            AutoSize = false,
            Size = new Size(inner - 220, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Theme.Status(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg
        };
        _cancelButton = new ActionButton
        {
            Text = "Cancel",
            Style = ActionButtonStyle.Ghost,
            Size = new Size(86, 34),
            Location = new Point(ClientSize.Width - pad - 200, 578),
            BackColor = Theme.Bg
        };
        _cancelButton.Click += (_, _) => RequestClose();

        _saveButton = new ActionButton
        {
            Text = "Save",
            Style = ActionButtonStyle.Primary,
            Size = new Size(100, 34),
            Location = new Point(ClientSize.Width - pad - 100, 578),
            BackColor = Theme.Bg
        };
        _saveButton.Click += (_, _) => DoSave();

        Controls.Add(titleLabel);
        Controls.Add(subtitleLabel);
        Controls.Add(_insertImageButton);
        Controls.Add(_templateButton);
        Controls.Add(_resetButton);
        Controls.Add(editorLabel);
        Controls.Add(_editorCard);
        Controls.Add(previewLabel);
        Controls.Add(_previewCard);
        Controls.Add(tip);
        Controls.Add(_statusLabel);
        Controls.Add(_cancelButton);
        Controls.Add(_saveButton);

        Paint += DrawChrome;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                RequestClose();
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

    // ── Editor operations ───────────────────────────────────────────────

    private void UpdatePreview()
    {
        try
        {
            // DocumentText set after the handle exists; WebBrowser handles re-render.
            _preview.DocumentText = WrapForPreview(_htmlEditor.Text);
        }
        catch
        {
            // Bad HTML will not crash the form. Worst case the preview is blank.
        }
    }

    private static string WrapForPreview(string body) =>
        "<!doctype html><html><head><meta charset=\"utf-8\">" +
        "<style>html,body{margin:0;padding:14px;background:#ffffff;color:#1f2937;font-family:'Segoe UI',Arial,sans-serif;}</style>" +
        "</head><body>" + body + "</body></html>";

    private void InsertImageFromFile()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Choose an image to embed",
            Filter = "Images (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All files (*.*)|*.*",
            CheckFileExists = true
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var bytes = File.ReadAllBytes(dlg.FileName);
            var mime  = MimeFromExtension(Path.GetExtension(dlg.FileName));
            var b64   = Convert.ToBase64String(bytes);
            var altText = Path.GetFileNameWithoutExtension(dlg.FileName);
            var imgTag = $"<img src=\"data:{mime};base64,{b64}\" alt=\"{WebUtility.HtmlEncode(altText)}\" style=\"max-width: 200px; height: auto;\">";

            var start = _htmlEditor.SelectionStart;
            _htmlEditor.Text = _htmlEditor.Text.Insert(start, imgTag);
            _htmlEditor.SelectionStart = start + imgTag.Length;
            _htmlEditor.Focus();

            if (bytes.Length > 200 * 1024)
            {
                _statusLabel.Text = $"Image inserted ({bytes.Length / 1024} KB). Large images may bloat your signature.";
                _statusLabel.ForeColor = Theme.AccentSoft;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not embed image: " + ex.Message,
                "QuickReply", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static string MimeFromExtension(string ext) => ext.ToLowerInvariant() switch
    {
        ".png"            => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif"            => "image/gif",
        ".bmp"            => "image/bmp",
        _                 => "application/octet-stream"
    };

    private void ShowTemplateMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Default (with contact info)", null, (_, _) =>
            ReplaceWithTemplate(SignatureService.DefaultTemplateHtml));
        menu.Items.Add("Minimal", null, (_, _) =>
            ReplaceWithTemplate(SignatureService.MinimalTemplateHtml));
        menu.Items.Add("With logo placeholder", null, (_, _) =>
            ReplaceWithTemplate(SignatureService.WithLogoTemplateHtml));
        menu.Show(_templateButton, new Point(0, _templateButton.Height));
    }

    private void ReplaceWithTemplate(string html)
    {
        if (_isDirty)
        {
            var choice = MessageBox.Show(this,
                "Replace your current edits with this template?\nUnsaved changes will be lost.",
                "QuickReply",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (choice != DialogResult.Yes) return;
        }
        _htmlEditor.Text = html;
    }

    private void DoSave()
    {
        if (_signatures.Save(_htmlEditor.Text))
        {
            _isDirty = false;
            _statusLabel.Text = $"Saved {DateTime.Now:HH:mm:ss}.";
            _statusLabel.ForeColor = Theme.Success;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void RequestClose()
    {
        if (_isDirty)
        {
            var choice = MessageBox.Show(this,
                "You have unsaved changes. Discard them and close?",
                "QuickReply",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (choice != DialogResult.Yes) return;
        }
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void UpdateStatus()
    {
        var byteCount = System.Text.Encoding.UTF8.GetByteCount(_htmlEditor.Text);
        var size = byteCount switch
        {
            < 1024            => $"{byteCount} bytes",
            < 1024 * 1024     => $"{byteCount / 1024} KB",
            _                 => $"{byteCount / 1024 / 1024} MB"
        };
        _statusLabel.Text = (_isDirty ? "Unsaved changes  ·  " : "") + size + "  ·  Ctrl+Enter to save";
        _statusLabel.ForeColor = _isDirty ? Theme.AccentSoft : Theme.TextMuted;
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

    // Drag by header
    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x84;
        const int HTCLIENT = 1;
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
