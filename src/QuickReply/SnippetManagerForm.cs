using System.Drawing.Drawing2D;

namespace QuickReply;

public class SnippetManagerForm : Form
{
    private readonly SnippetService _snippets;

    private DataGridView _grid = null!;
    private ActionButton _addButton = null!;
    private ActionButton _editButton = null!;
    private ActionButton _deleteButton = null!;
    private ActionButton _closeButton = null!;
    private Label _countLabel = null!;
    private TextBox _filterBox = null!;
    private CardPanel _filterCard = null!;

    public SnippetManagerForm(SnippetService snippets)
    {
        _snippets = snippets;
        InitializeUi();
        RefreshGrid();

        _snippets.Reloaded += OnSnippetsReloaded;
        FormClosed += (_, _) => _snippets.Reloaded -= OnSnippetsReloaded;
    }

    private void OnSnippetsReloaded(object? sender, EventArgs e)
    {
        if (IsHandleCreated) BeginInvoke(new Action(RefreshGrid));
    }

    private void InitializeUi()
    {
        SuspendLayout();

        Text = "QuickReply - Manage Snippets";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.75f);
        ClientSize = new Size(820, 580);
        DoubleBuffered = true;

        const int pad = 24;
        var inner = ClientSize.Width - pad * 2;

        // ── Header ────────────────────────────────────────────────────────
        var titleLabel = new Label
        {
            Text = "Manage Snippets",
            Font = Theme.H1(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad, 20)
        };
        _countLabel = new Label
        {
            Text = "",
            Font = Theme.Subtitle(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 50)
        };

        // Filter
        _filterCard = new CardPanel
        {
            Location = new Point(pad, 96),
            Size = new Size(inner, 38),
            FillColor = Theme.Surface,
            BorderColor = Theme.Border,
            BackColor = Theme.Bg
        };
        var searchIcon = new Label
        {
            Text = "🔍",
            Font = new Font("Segoe UI", 10f),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Surface,
            AutoSize = false,
            Size = new Size(28, _filterCard.Height),
            Location = new Point(6, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _filterBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.Body(),
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            PlaceholderText = "Filter by code or text...",
            Location = new Point(36, 10),
            Width = inner - 50
        };
        _filterBox.TextChanged += (_, _) => RefreshGrid();
        _filterBox.GotFocus  += (_, _) => { _filterCard.IsFocused = true;  _filterCard.Invalidate(); };
        _filterBox.LostFocus += (_, _) => { _filterCard.IsFocused = false; _filterCard.Invalidate(); };
        _filterCard.Controls.Add(searchIcon);
        _filterCard.Controls.Add(_filterBox);

        // Grid
        _grid = new DataGridView
        {
            Location = new Point(pad, 148),
            Size = new Size(inner, 340),
            BackgroundColor = Theme.BgRaised,
            BorderStyle = BorderStyle.FixedSingle,
            GridColor = Theme.Border,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToOrderColumns = false,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ColumnHeadersHeight = 30,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextMuted,
                Font = Theme.Caps(),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                SelectionBackColor = Theme.Surface,
                SelectionForeColor = Theme.TextMuted
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.BgRaised,
                ForeColor = Theme.Text,
                Font = Theme.Body(),
                SelectionBackColor = Theme.AccentBg,
                SelectionForeColor = Theme.Text,
                Padding = new Padding(6, 0, 0, 0)
            },
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 28 }
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "CODE",
            Name = "Code",
            Width = 140,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "TYPE",
            Name = "Type",
            Width = 130,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "PREVIEW",
            Name = "Preview",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) EditSelected();
        };
        _grid.SelectionChanged += (_, _) => UpdateButtonState();
        _grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete)
            {
                e.SuppressKeyPress = true;
                DeleteSelected();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                EditSelected();
            }
        };

        // ── Footer ────────────────────────────────────────────────────────
        var hintLabel = new Label
        {
            Text = "Double-click a row to edit. Delete removes the entry.",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(pad + 2, 502)
        };

        _addButton = new ActionButton
        {
            Text = "+  Add",
            Style = ActionButtonStyle.Secondary,
            Size = new Size(96, 34),
            Location = new Point(pad, 530),
            BackColor = Theme.Bg
        };
        _addButton.Click += (_, _) =>
        {
            using var editor = new AddSnippetForm(_snippets);
            editor.ShowDialog(this);
            // RefreshGrid is fired via Reloaded event.
        };

        _editButton = new ActionButton
        {
            Text = "Edit",
            Style = ActionButtonStyle.Secondary,
            Size = new Size(80, 34),
            Location = new Point(pad + 104, 530),
            BackColor = Theme.Bg
        };
        _editButton.Click += (_, _) => EditSelected();

        _deleteButton = new ActionButton
        {
            Text = "Delete",
            Style = ActionButtonStyle.Ghost,
            Size = new Size(86, 34),
            Location = new Point(pad + 192, 530),
            BackColor = Theme.Bg
        };
        _deleteButton.Click += (_, _) => DeleteSelected();

        _closeButton = new ActionButton
        {
            Text = "Close",
            Style = ActionButtonStyle.Primary,
            Size = new Size(96, 34),
            Location = new Point(ClientSize.Width - pad - 96, 530),
            BackColor = Theme.Bg
        };
        _closeButton.Click += (_, _) => Close();

        Controls.Add(titleLabel);
        Controls.Add(_countLabel);
        Controls.Add(_filterCard);
        Controls.Add(_grid);
        Controls.Add(hintLabel);
        Controls.Add(_addButton);
        Controls.Add(_editButton);
        Controls.Add(_deleteButton);
        Controls.Add(_closeButton);

        Paint += DrawChrome;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
        };

        ResumeLayout(false);
        PerformLayout();

        UpdateButtonState();
    }

    private void RefreshGrid()
    {
        var filter = _filterBox?.Text?.Trim() ?? string.Empty;
        var entries = _snippets.Snippets
            .Where(kv => MatchesFilter(kv.Key, kv.Value, filter))
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _grid.Rows.Clear();
        foreach (var (code, entry) in entries)
        {
            var type = entry.IsAlias
                ? $"alias  ->  {entry.AliasTarget}"
                : entry.Variants.Length == 1
                    ? "single"
                    : $"{entry.Variants.Length} variants";

            var preview = entry.IsAlias
                ? string.Empty
                : Truncate(entry.Variants.Length == 0 ? "" : entry.Variants[0], 110);

            _grid.Rows.Add(code, type, preview);
        }

        _countLabel.Text = _snippets.Snippets.Count == 1
            ? "1 snippet"
            : $"{_snippets.Snippets.Count} snippets total" +
              (entries.Count != _snippets.Snippets.Count
                  ? $"  ·  {entries.Count} shown"
                  : "");

        UpdateButtonState();
    }

    private static bool MatchesFilter(string code, SnippetEntry entry, string filter)
    {
        if (string.IsNullOrEmpty(filter)) return true;
        if (code.Contains(filter, StringComparison.OrdinalIgnoreCase)) return true;
        if (entry.IsAlias)
            return entry.AliasTarget?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false;
        foreach (var v in entry.Variants)
        {
            if (v.Contains(filter, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var oneLine = s.ReplaceLineEndings(" ").Trim();
        return oneLine.Length <= max ? oneLine : oneLine[..(max - 1)] + "…";
    }

    private void UpdateButtonState()
    {
        var hasSelection = _grid.SelectedRows.Count > 0
                         && _grid.SelectedRows[0].Cells["Code"].Value is string;
        _editButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection;
    }

    private string? GetSelectedCode()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        return _grid.SelectedRows[0].Cells["Code"].Value as string;
    }

    private void EditSelected()
    {
        var code = GetSelectedCode();
        if (string.IsNullOrEmpty(code)) return;
        using var editor = new AddSnippetForm(_snippets, prefillCode: code);
        editor.ShowDialog(this);
        // Refresh handled by Reloaded event when AddOrUpdate runs.
    }

    private void DeleteSelected()
    {
        var code = GetSelectedCode();
        if (string.IsNullOrEmpty(code)) return;

        var confirm = MessageBox.Show(this,
            $"Delete the snippet \"{code}\"?\nThis cannot be undone.",
            "QuickReply",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        _snippets.Remove(code);
        // RefreshGrid via Reloaded event.
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
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int CS_DROPSHADOW    = 0x00020000;
            cp.ExStyle    |= WS_EX_TOOLWINDOW;
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
