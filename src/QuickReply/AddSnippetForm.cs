using System.Drawing.Drawing2D;

namespace QuickReply;

public class AddSnippetForm : Form
{
    private readonly SnippetService _snippets;
    private readonly string? _prefillCode;
    private readonly string[]? _prefillVariants;

    private TextBox _codeInput = null!;
    private CardPanel _codeCard = null!;
    private Panel _variantsScroll = null!;
    private FlowLayoutPanel _variantsList = null!;
    private ActionButton _addVariantButton = null!;
    private ActionButton _saveButton = null!;
    private ActionButton _cancelButton = null!;
    private Label _statusLabel = null!;
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private Label _aliasWarningLabel = null!;
    private Label _variantsHint = null!;

    public string? SavedCode { get; private set; }

    public AddSnippetForm(SnippetService snippets, string? prefillCode = null, string[]? prefillVariants = null)
    {
        _snippets = snippets;
        _prefillCode = prefillCode;
        _prefillVariants = prefillVariants;
        InitializeUi();

        if (!string.IsNullOrWhiteSpace(_prefillCode))
        {
            _codeInput.Text = _prefillCode;
            _codeInput.SelectionStart = _codeInput.Text.Length;
        }

        // If the caller passed explicit variants, load them. Otherwise check
        // whether the prefill code is an existing snippet and load its variants.
        if (_prefillVariants is { Length: > 0 })
        {
            foreach (var v in _prefillVariants) AddVariantRow(v);
        }
        else if (!string.IsNullOrWhiteSpace(_prefillCode)
                 && _snippets.Snippets.TryGetValue(_prefillCode, out var existing))
        {
            if (existing.IsAlias)
            {
                _aliasWarningLabel.Text =
                    $"This code is currently an alias to \"{existing.AliasTarget}\". " +
                    "Saving will replace the alias with the variants below.";
                _aliasWarningLabel.Visible = true;
                AddVariantRow(string.Empty);
            }
            else
            {
                foreach (var v in existing.Variants) AddVariantRow(v);
            }
        }
        else
        {
            AddVariantRow(string.Empty);
        }

        UpdateStatus();
    }

    private void InitializeUi()
    {
        SuspendLayout();

        Text = "QuickReply - Edit Snippet";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.75f);
        ClientSize = new Size(620, 640);
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
            Text = "Create a code and one or more reply variants.",
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
                if (_variantsList.Controls.Count > 0)
                {
                    var firstRow = _variantsList.Controls[0];
                    var tb = firstRow.Controls.OfType<CardPanel>().FirstOrDefault()?.Controls.OfType<TextBox>().FirstOrDefault();
                    tb?.Focus();
                }
            }
        };
        _codeCard.Controls.Add(_codeInput);

        _aliasWarningLabel = new Label
        {
            Location = new Point(pad + 2, 168),
            Size = new Size(inner - 4, 32),
            AutoSize = false,
            Font = Theme.Status(),
            ForeColor = Theme.AccentSoft,
            BackColor = Theme.Bg,
            TextAlign = ContentAlignment.MiddleLeft,
            Visible = false
        };

        // ── Variants ──────────────────────────────────────────────────────
        var variantsLabel = new Label
        {
            Text = "REPLY VARIANTS",
            Font = Theme.Caps(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 200)
        };
        _variantsHint = new Label
        {
            Text = "When this code is used, one variant is picked at random.",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 220)
        };

        _variantsScroll = new Panel
        {
            Location = new Point(pad, 246),
            Size = new Size(inner, 280),
            BackColor = Theme.Bg,
            AutoScroll = true
        };
        _variantsList = new FlowLayoutPanel
        {
            Location = new Point(0, 0),
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Bg,
            WrapContents = false
        };
        _variantsScroll.Controls.Add(_variantsList);

        _addVariantButton = new ActionButton
        {
            Text = "+  Add variant",
            Style = ActionButtonStyle.Secondary,
            Size = new Size(140, 30),
            Location = new Point(pad, 534),
            BackColor = Theme.Bg
        };
        _addVariantButton.Click += (_, _) =>
        {
            AddVariantRow(string.Empty);
            FocusLastVariant();
        };

        // ── Footer ────────────────────────────────────────────────────────
        var tipLabel = new Label
        {
            Text = "Tip: {{date:yyyy-MM-dd}} inserts today's date. Ctrl+Enter saves.",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 574)
        };

        _statusLabel = new Label
        {
            Location = new Point(pad + 2, 600),
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
            Location = new Point(ClientSize.Width - pad - 174, 597),
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
            Location = new Point(ClientSize.Width - pad - 86, 597),
            BackColor = Theme.Bg
        };
        _saveButton.Click += (_, _) => DoSave();

        Controls.Add(_titleLabel);
        Controls.Add(_subtitleLabel);
        Controls.Add(codeLabel);
        Controls.Add(_codeCard);
        Controls.Add(_aliasWarningLabel);
        Controls.Add(variantsLabel);
        Controls.Add(_variantsHint);
        Controls.Add(_variantsScroll);
        Controls.Add(_addVariantButton);
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

    private void AddVariantRow(string initialText)
    {
        var row = new Panel
        {
            Size = new Size(_variantsScroll.ClientSize.Width - 4, 96),
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Theme.Bg
        };

        var numberLabel = new Label
        {
            Text = "1.",
            Font = Theme.ChipCode(),
            ForeColor = Theme.AccentSoft,
            BackColor = Theme.Bg,
            AutoSize = false,
            Size = new Size(28, 24),
            Location = new Point(0, 2),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var card = new CardPanel
        {
            Location = new Point(30, 0),
            Size = new Size(row.Width - 122, 88),
            FillColor = Theme.BgRaised,
            BorderColor = Theme.Border,
            BackColor = Theme.Bg
        };
        var tb = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.BgRaised,
            ForeColor = Theme.Text,
            Font = Theme.Preview(),
            WordWrap = true,
            AcceptsReturn = true,
            Text = initialText,
            Location = new Point(12, 10),
            Size = new Size(card.Width - 24, card.Height - 20)
        };
        tb.GotFocus  += (_, _) => { card.IsFocused = true;  card.Invalidate(); };
        tb.LostFocus += (_, _) => { card.IsFocused = false; card.Invalidate(); };
        card.Controls.Add(tb);

        var removeButton = new ActionButton
        {
            Text = "Remove",
            Style = ActionButtonStyle.Ghost,
            Size = new Size(80, 28),
            Location = new Point(row.Width - 84, 30),
            BackColor = Theme.Bg
        };
        removeButton.Click += (_, _) =>
        {
            _variantsList.Controls.Remove(row);
            row.Dispose();
            EnsureAtLeastOneRow();
            RenumberRows();
        };

        row.Controls.Add(numberLabel);
        row.Controls.Add(card);
        row.Controls.Add(removeButton);
        _variantsList.Controls.Add(row);
        RenumberRows();
    }

    private void EnsureAtLeastOneRow()
    {
        if (_variantsList.Controls.Count == 0)
        {
            AddVariantRow(string.Empty);
        }
    }

    private void RenumberRows()
    {
        for (var i = 0; i < _variantsList.Controls.Count; i++)
        {
            if (_variantsList.Controls[i].Controls.OfType<Label>().FirstOrDefault() is { } lbl)
            {
                lbl.Text = (i + 1) + ".";
            }
        }
        UpdateVariantsHint();
    }

    private void UpdateVariantsHint()
    {
        var n = _variantsList.Controls.Count;
        _variantsHint.Text = n switch
        {
            <= 1 => "When this code is used, this single reply is pasted.",
            _    => $"When this code is used, one of these {n} variants is picked at random."
        };
    }

    private void FocusLastVariant()
    {
        if (_variantsList.Controls.Count == 0) return;
        var lastRow = _variantsList.Controls[^1];
        var card = lastRow.Controls.OfType<CardPanel>().FirstOrDefault();
        card?.Controls.OfType<TextBox>().FirstOrDefault()?.Focus();
        _variantsScroll.ScrollControlIntoView(lastRow);
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
            _subtitleLabel.Text = "Create a code and one or more reply variants.";
            return;
        }
        if (_snippets.Contains(code))
        {
            _statusLabel.Text = $"●  \"{code}\" exists. Saving will replace it.";
            _statusLabel.ForeColor = Theme.AccentSoft;
            _titleLabel.Text = "Edit Snippet";
            _subtitleLabel.Text = $"Update the reply text for \"{code}\".";
        }
        else
        {
            _statusLabel.Text = "●  new snippet";
            _statusLabel.ForeColor = Theme.Success;
            _titleLabel.Text = "Add Snippet";
            _subtitleLabel.Text = "Create a code and one or more reply variants.";
        }
        Invalidate();
    }

    private List<string> CollectVariants()
    {
        var result = new List<string>();
        foreach (Control row in _variantsList.Controls)
        {
            var card = row.Controls.OfType<CardPanel>().FirstOrDefault();
            var tb = card?.Controls.OfType<TextBox>().FirstOrDefault();
            var text = tb?.Text ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
            {
                result.Add(text);
            }
        }
        return result;
    }

    private void DoSave()
    {
        var code = _codeInput.Text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            _statusLabel.Text = "Code cannot be empty.";
            _statusLabel.ForeColor = Theme.Danger;
            _codeInput.Focus();
            return;
        }

        var variants = CollectVariants();
        if (variants.Count == 0)
        {
            _statusLabel.Text = "Add at least one reply variant.";
            _statusLabel.ForeColor = Theme.Danger;
            FocusLastVariant();
            return;
        }

        if (_snippets.AddOrUpdate(code, variants))
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
