using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace QuickReply.Setup;

public class SetupWizardForm : Form
{
    private readonly SetupChoices _choices = new();
    private readonly Installer _installer = new();

    private readonly List<Page> _pages = new();
    private int _currentPageIndex;

    // Chrome
    private Label _titleLabel = null!;
    private Label _stepLabel = null!;
    private ActionButton _backButton = null!;
    private ActionButton _nextButton = null!;
    private ActionButton _cancelButton = null!;

    // Per-page controls (need refs so we can read/write state on transitions)
    private TextBox _installPathBox = null!;
    private RadioButton _hotkeyDefaultRadio = null!;
    private RadioButton _hotkeyCustomRadio = null!;
    private TextBox _hotkeyCustomBox = null!;
    private Label _hotkeyValidationLabel = null!;
    private RadioButton _snippetsDefaultRadio = null!;
    private RadioButton _snippetsEmptyRadio = null!;
    private RadioButton _snippetsCustomRadio = null!;
    private DataGridView _snippetsGrid = null!;
    private CheckBox _startupCheckbox = null!;
    private CheckBox _randomizeCheckbox = null!;
    private CheckBox _portableCheckbox = null!;
    private Label _summaryLabel = null!;
    private ProgressBar _installProgress = null!;
    private Label _installStatusLabel = null!;
    private Label _installErrorLabel = null!;
    private CheckBox _launchAfterInstall = null!;
    private Label _doneLabel = null!;

    public SetupWizardForm()
    {
        InitializeUi();
        ShowPage(0);
    }

    private void InitializeUi()
    {
        Text = "QuickReply Setup";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.75f);
        ClientSize = new Size(760, 580);
        DoubleBuffered = true;
        KeyPreview = true;

        BuildChrome();
        BuildPages();

        Paint += DrawBorder;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) RequestCancel();
        };
    }

    private void BuildChrome()
    {
        _titleLabel = new Label
        {
            Text = "QuickReply Setup",
            Font = Theme.H1(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(24, 20)
        };
        _stepLabel = new Label
        {
            Text = "Step 1 of 7",
            Font = Theme.Subtitle(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(26, 50)
        };

        _backButton = new ActionButton
        {
            Text = "< Back",
            Style = ActionButtonStyle.Ghost,
            Size = new Size(80, 34),
            Location = new Point(24, ClientSize.Height - 58),
            BackColor = Theme.Bg
        };
        _backButton.Click += (_, _) => GoBack();

        _cancelButton = new ActionButton
        {
            Text = "Cancel",
            Style = ActionButtonStyle.Ghost,
            Size = new Size(80, 34),
            Location = new Point(ClientSize.Width - 24 - 184, ClientSize.Height - 58),
            BackColor = Theme.Bg
        };
        _cancelButton.Click += (_, _) => RequestCancel();

        _nextButton = new ActionButton
        {
            Text = "Next >",
            Style = ActionButtonStyle.Primary,
            Size = new Size(94, 34),
            Location = new Point(ClientSize.Width - 24 - 94, ClientSize.Height - 58),
            BackColor = Theme.Bg
        };
        _nextButton.Click += (_, _) => GoNext();

        Controls.Add(_titleLabel);
        Controls.Add(_stepLabel);
        Controls.Add(_backButton);
        Controls.Add(_cancelButton);
        Controls.Add(_nextButton);
    }

    private void BuildPages()
    {
        _pages.Add(BuildWelcomePage());
        _pages.Add(BuildInstallLocationPage());
        _pages.Add(BuildHotkeyPage());
        _pages.Add(BuildSnippetsPage());
        _pages.Add(BuildStartupPage());
        _pages.Add(BuildSummaryPage());
        _pages.Add(BuildInstallingPage());
        _pages.Add(BuildDonePage());

        foreach (var p in _pages)
        {
            p.Container.Visible = false;
            p.Container.Location = new Point(24, 92);
            p.Container.Size = new Size(ClientSize.Width - 48, ClientSize.Height - 168);
            p.Container.BackColor = Theme.Bg;
            Controls.Add(p.Container);
        }
    }

    private Page BuildWelcomePage()
    {
        var panel = new Panel();
        var headline = new Label
        {
            Text = "Welcome to QuickReply.",
            Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 24)
        };
        var body = new Label
        {
            Text =
                "QuickReply is a small Windows tray utility for service desk and ticket\n" +
                "response snippets. Press a hotkey, type a short code, paste a polished\n" +
                "reply.\n\n" +
                "This setup wizard will:\n\n" +
                "    1. Ask where to install QuickReply.\n" +
                "    2. Let you keep the default Ctrl+Alt+; hotkey or pick your own.\n" +
                "    3. Let you keep the included starter snippets or define your own.\n" +
                "    4. Optionally start QuickReply automatically with Windows.\n" +
                "    5. Download the latest QuickReply.exe from GitHub and configure it.",
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 76)
        };
        panel.Controls.Add(headline);
        panel.Controls.Add(body);
        return new Page(panel, "Step 1 of 7", "Welcome");
    }

    private Page BuildInstallLocationPage()
    {
        var panel = new Panel();
        AddPageHeader(panel, "Install location",
            "Choose where QuickReply.exe and its config files will live. The default\n" +
            "is a per-user folder, so no administrator privileges are required.");

        var card = new CardPanel
        {
            Location = new Point(0, 130),
            Size = new Size(panel.ClientSize.Width > 0 ? panel.ClientSize.Width : 700, 46),
            Width = 700,
            FillColor = Theme.Surface,
            BorderColor = Theme.Border,
            BackColor = Theme.Bg
        };
        _installPathBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.BodyLg(),
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            Text = _choices.InstallPath,
            Location = new Point(14, 13),
            Width = 580
        };
        _installPathBox.GotFocus  += (_, _) => { card.IsFocused = true;  card.Invalidate(); };
        _installPathBox.LostFocus += (_, _) => { card.IsFocused = false; card.Invalidate(); };
        card.Controls.Add(_installPathBox);

        var browseButton = new ActionButton
        {
            Text = "Browse...",
            Style = ActionButtonStyle.Secondary,
            Size = new Size(94, 30),
            Location = new Point(card.Width - 94 - 8, 8),
            BackColor = Theme.Surface
        };
        browseButton.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Choose an install location for QuickReply.",
                SelectedPath = Directory.Exists(_installPathBox.Text)
                    ? _installPathBox.Text
                    : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                UseDescriptionForTitle = true
            };
            if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
            {
                _installPathBox.Text = Path.Combine(dlg.SelectedPath, "QuickReply");
            }
        };
        card.Controls.Add(browseButton);

        var hint = new Label
        {
            Text = $"Tip: %LOCALAPPDATA%\\Programs\\QuickReply is the recommended default.",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(2, 190)
        };

        panel.Controls.Add(card);
        panel.Controls.Add(hint);
        return new Page(panel, "Step 2 of 7", "Install location");
    }

    private Page BuildHotkeyPage()
    {
        var panel = new Panel();
        AddPageHeader(panel, "Hotkey",
            "QuickReply opens its picker from a global hotkey. Keep the default,\n" +
            "or pick a combination that does not collide with another app.");

        _hotkeyDefaultRadio = MakeRadio("Use the default:  Ctrl + Alt + ;", new Point(0, 130));
        _hotkeyDefaultRadio.Checked = _choices.Hotkey.Equals("Ctrl+Alt+;", StringComparison.OrdinalIgnoreCase);

        _hotkeyCustomRadio  = MakeRadio("Use a custom hotkey:", new Point(0, 168));
        _hotkeyCustomRadio.Checked = !_hotkeyDefaultRadio.Checked;

        var card = new CardPanel
        {
            Location = new Point(26, 200),
            Size = new Size(674, 46),
            FillColor = Theme.Surface,
            BorderColor = Theme.Border,
            BackColor = Theme.Bg
        };
        _hotkeyCustomBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.BodyLg(),
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            PlaceholderText = "e.g. Ctrl+Shift+Space",
            Text = _hotkeyDefaultRadio.Checked ? "" : _choices.Hotkey,
            Location = new Point(14, 13),
            Width = 644
        };
        _hotkeyCustomBox.GotFocus  += (_, _) => { card.IsFocused = true;  card.Invalidate(); };
        _hotkeyCustomBox.LostFocus += (_, _) => { card.IsFocused = false; card.Invalidate(); };
        _hotkeyCustomBox.TextChanged += (_, _) =>
        {
            if (_hotkeyCustomRadio.Checked) ValidateHotkey(_hotkeyCustomBox.Text);
        };
        card.Controls.Add(_hotkeyCustomBox);

        _hotkeyValidationLabel = new Label
        {
            Location = new Point(26, 256),
            AutoSize = true,
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            Text = "Combine Ctrl, Alt, Shift, or Win with a letter, digit, punctuation, or F1-F12."
        };

        _hotkeyDefaultRadio.CheckedChanged += (_, _) =>
        {
            if (_hotkeyDefaultRadio.Checked)
            {
                _hotkeyValidationLabel.Text = "Default hotkey selected.";
                _hotkeyValidationLabel.ForeColor = Theme.Success;
            }
        };
        _hotkeyCustomRadio.CheckedChanged += (_, _) =>
        {
            if (_hotkeyCustomRadio.Checked)
            {
                ValidateHotkey(_hotkeyCustomBox.Text);
                _hotkeyCustomBox.Focus();
            }
        };

        panel.Controls.Add(_hotkeyDefaultRadio);
        panel.Controls.Add(_hotkeyCustomRadio);
        panel.Controls.Add(card);
        panel.Controls.Add(_hotkeyValidationLabel);
        return new Page(panel, "Step 3 of 7", "Hotkey");
    }

    private Page BuildSnippetsPage()
    {
        var panel = new Panel();
        AddPageHeader(panel, "Snippets",
            "Start with the included service-desk snippets, or define your own.\n" +
            "You can edit, add, or remove snippets anytime after install.");

        _snippetsDefaultRadio = MakeRadio("Use the 36 included starter snippets  (recommended)", new Point(0, 130));
        _snippetsDefaultRadio.Checked = true;
        _snippetsEmptyRadio   = MakeRadio("Start with no snippets",                                  new Point(0, 162));
        _snippetsCustomRadio  = MakeRadio("Define my own snippets now",                              new Point(0, 194));

        _snippetsGrid = new DataGridView
        {
            Location = new Point(26, 230),
            Size = new Size(674, 200),
            BackgroundColor = Theme.BgRaised,
            BorderStyle = BorderStyle.FixedSingle,
            GridColor = Theme.Border,
            RowHeadersVisible = false,
            AllowUserToResizeRows = false,
            AllowUserToAddRows = true,
            ColumnHeadersHeight = 28,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextMuted,
                Font = Theme.Caps(),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
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
                Padding = new Padding(4, 0, 0, 0)
            },
            EnableHeadersVisualStyles = false,
            Visible = false
        };
        _snippetsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "CODE",
            Name = "Code",
            Width = 120,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _snippetsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "REPLY TEXT",
            Name = "Text",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        _snippetsCustomRadio.CheckedChanged += (_, _) =>
        {
            _snippetsGrid.Visible = _snippetsCustomRadio.Checked;
        };

        panel.Controls.Add(_snippetsDefaultRadio);
        panel.Controls.Add(_snippetsEmptyRadio);
        panel.Controls.Add(_snippetsCustomRadio);
        panel.Controls.Add(_snippetsGrid);
        return new Page(panel, "Step 4 of 7", "Snippets");
    }

    private Page BuildStartupPage()
    {
        var panel = new Panel();
        AddPageHeader(panel, "A couple of preferences",
            "Two small choices about how QuickReply behaves after it is installed.\n" +
            "Both can be changed later in appsettings.json.");

        // ── Windows startup ───────────────────────────────────────────────
        var startupHeading = new Label
        {
            Text = "WINDOWS STARTUP",
            Font = Theme.Caps(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 120)
        };
        _startupCheckbox = new CheckBox
        {
            Text = "Start QuickReply automatically when I sign in to Windows",
            Checked = true,
            AutoSize = true,
            Location = new Point(0, 142),
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            FlatStyle = FlatStyle.Standard
        };
        var startupHint = new Label
        {
            Text = "Writes a single value under HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run.\nNo admin privileges required. You can remove it from Task Manager > Startup later.",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(2, 172)
        };

        // ── Randomized responses ──────────────────────────────────────────
        var randomHeading = new Label
        {
            Text = "RANDOMIZED RESPONSES",
            Font = Theme.Caps(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 220)
        };
        _randomizeCheckbox = new CheckBox
        {
            Text = "Pick a random variant each time a code has multiple replies",
            Checked = true,
            AutoSize = true,
            Location = new Point(0, 242),
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            FlatStyle = FlatStyle.Standard
        };
        var randomHint = new Label
        {
            Text = "Many of the included snippets ship with 8 different ways to say the same thing.\nDisable to always paste the first variant.",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(2, 270)
        };

        // ── Portable mode ────────────────────────────────────────────────
        var portableHeading = new Label
        {
            Text = "DATA LOCATION",
            Font = Theme.Caps(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 314)
        };
        _portableCheckbox = new CheckBox
        {
            Text = "Portable mode: keep snippets and settings next to QuickReply.exe",
            Checked = false,
            AutoSize = true,
            Location = new Point(0, 336),
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            FlatStyle = FlatStyle.Standard
        };
        var portableHint = new Label
        {
            Text = "By default, snippets.json, appsettings.json, and signature.html live in\n%APPDATA%\\QuickReply so they survive reinstalls and roam in domain profiles.\nEnable portable mode for a self-contained USB / single-folder install.",
            Font = Theme.Status(),
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(2, 364)
        };

        panel.Controls.Add(startupHeading);
        panel.Controls.Add(_startupCheckbox);
        panel.Controls.Add(startupHint);
        panel.Controls.Add(randomHeading);
        panel.Controls.Add(_randomizeCheckbox);
        panel.Controls.Add(randomHint);
        panel.Controls.Add(portableHeading);
        panel.Controls.Add(_portableCheckbox);
        panel.Controls.Add(portableHint);
        return new Page(panel, "Step 5 of 7", "Preferences");
    }

    private Page BuildSummaryPage()
    {
        var panel = new Panel();
        AddPageHeader(panel, "Ready to install",
            "Review your choices. Click Install to download QuickReply.exe from\n" +
            "GitHub and apply your settings.");

        _summaryLabel = new Label
        {
            Location = new Point(0, 130),
            AutoSize = false,
            Size = new Size(700, 240),
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            TextAlign = ContentAlignment.TopLeft
        };
        panel.Controls.Add(_summaryLabel);
        return new Page(panel, "Step 6 of 7", "Summary");
    }

    private Page BuildInstallingPage()
    {
        var panel = new Panel();
        AddPageHeader(panel, "Installing",
            "Downloading QuickReply.exe and writing your configuration.");

        _installStatusLabel = new Label
        {
            Location = new Point(0, 150),
            AutoSize = false,
            Size = new Size(700, 24),
            Font = Theme.Body(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            Text = "Preparing..."
        };
        _installProgress = new ProgressBar
        {
            Location = new Point(0, 184),
            Size = new Size(700, 18),
            Style = ProgressBarStyle.Continuous,
            Minimum = 0,
            Maximum = 100,
            Value = 0
        };
        _installErrorLabel = new Label
        {
            Location = new Point(0, 220),
            AutoSize = false,
            Size = new Size(700, 120),
            Font = Theme.Body(),
            ForeColor = Theme.Danger,
            BackColor = Theme.Bg,
            Visible = false
        };
        panel.Controls.Add(_installStatusLabel);
        panel.Controls.Add(_installProgress);
        panel.Controls.Add(_installErrorLabel);
        return new Page(panel, "Step 7 of 7", "Installing");
    }

    private Page BuildDonePage()
    {
        var panel = new Panel();
        var headline = new Label
        {
            Text = "QuickReply is installed.",
            Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
            ForeColor = Theme.Success,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 24)
        };
        _doneLabel = new Label
        {
            Location = new Point(0, 70),
            AutoSize = false,
            Size = new Size(700, 160),
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            TextAlign = ContentAlignment.TopLeft
        };
        _launchAfterInstall = new CheckBox
        {
            Text = "Launch QuickReply now",
            Checked = true,
            AutoSize = true,
            Location = new Point(0, 240),
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg
        };
        panel.Controls.Add(headline);
        panel.Controls.Add(_doneLabel);
        panel.Controls.Add(_launchAfterInstall);
        return new Page(panel, "Done", "Done");
    }

    // --- Page navigation ---------------------------------------------------

    private void ShowPage(int index)
    {
        for (var i = 0; i < _pages.Count; i++) _pages[i].Container.Visible = (i == index);
        _currentPageIndex = index;
        _stepLabel.Text = _pages[index].StepLabel;

        // Page-entry hooks
        if (index == IndexOf("Summary")) RefreshSummary();
        if (index == IndexOf("Done")) RefreshDone();

        // Button state
        _backButton.Enabled = index > 0 && index < IndexOf("Installing");
        _backButton.Visible = index < IndexOf("Installing");

        if (index == IndexOf("Summary"))
        {
            _nextButton.Text = "Install";
        }
        else if (index == IndexOf("Installing"))
        {
            _nextButton.Visible = false;
            _backButton.Visible = false;
            _cancelButton.Visible = false;
        }
        else if (index == IndexOf("Done"))
        {
            _nextButton.Text = "Finish";
            _nextButton.Visible = true;
            _cancelButton.Visible = false;
        }
        else
        {
            _nextButton.Text = "Next >";
            _nextButton.Visible = true;
            _cancelButton.Visible = true;
        }
    }

    private void GoNext()
    {
        if (!CommitCurrentPage()) return;

        if (_currentPageIndex == IndexOf("Summary"))
        {
            ShowPage(IndexOf("Installing"));
            _ = RunInstallAsync();
            return;
        }
        if (_currentPageIndex == IndexOf("Done"))
        {
            FinishWizard();
            return;
        }
        ShowPage(_currentPageIndex + 1);
    }

    private void GoBack()
    {
        if (_currentPageIndex == 0) return;
        ShowPage(_currentPageIndex - 1);
    }

    private bool CommitCurrentPage()
    {
        var name = _pages[_currentPageIndex].Name;
        switch (name)
        {
            case "Install location":
                var path = _installPathBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(path))
                {
                    Warn("Choose an install location.");
                    return false;
                }
                try
                {
                    path = Path.GetFullPath(path);
                }
                catch
                {
                    Warn("That path is not valid.");
                    return false;
                }
                _choices.InstallPath = path;
                return true;

            case "Hotkey":
                if (_hotkeyDefaultRadio.Checked)
                {
                    _choices.Hotkey = "Ctrl+Alt+;";
                    return true;
                }
                var hk = _hotkeyCustomBox.Text.Trim();
                if (!IsHotkeyShapeValid(hk))
                {
                    Warn("Enter a hotkey like \"Ctrl+Alt+;\" (at least one modifier plus a key).");
                    return false;
                }
                _choices.Hotkey = hk;
                return true;

            case "Snippets":
                if (_snippetsDefaultRadio.Checked) _choices.Snippets = SnippetMode.Defaults;
                else if (_snippetsEmptyRadio.Checked) _choices.Snippets = SnippetMode.Empty;
                else
                {
                    _choices.Snippets = SnippetMode.Custom;
                    _choices.CustomSnippets.Clear();
                    foreach (DataGridViewRow row in _snippetsGrid.Rows)
                    {
                        if (row.IsNewRow) continue;
                        var code = (row.Cells["Code"].Value as string)?.Trim();
                        var text = (row.Cells["Text"].Value as string) ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(code)) continue;
                        _choices.CustomSnippets[code] = text;
                    }
                }
                return true;

            case "Preferences":
                _choices.RunOnStartup = _startupCheckbox.Checked;
                _choices.RandomizeResponses = _randomizeCheckbox.Checked;
                _choices.PortableMode = _portableCheckbox.Checked;
                return true;

            default:
                return true;
        }
    }

    private void RefreshSummary()
    {
        var snippetSummary = _choices.Snippets switch
        {
            SnippetMode.Defaults => "Use the 36 included starter snippets.",
            SnippetMode.Empty    => "Start with no snippets.",
            SnippetMode.Custom   => $"Use {_choices.CustomSnippets.Count} custom snippet"
                                    + (_choices.CustomSnippets.Count == 1 ? "." : "s."),
            _ => "Use the included snippets."
        };

        _summaryLabel.Text =
            $"Install location:\n    {_choices.InstallPath}\n\n" +
            $"Data location:\n    {_choices.ResolveDataDirectory()}{(_choices.PortableMode ? "  (portable mode)" : "")}\n\n" +
            $"Hotkey:\n    {_choices.Hotkey}\n\n" +
            $"Snippets:\n    {snippetSummary}\n\n" +
            $"Windows startup:\n    {(_choices.RunOnStartup ? "Yes, launch QuickReply when I sign in." : "No, I will launch QuickReply myself.")}\n\n" +
            $"Randomized responses:\n    {(_choices.RandomizeResponses ? "On. Pick a random variant when a code has multiple replies." : "Off. Always paste the first variant.")}";
    }

    private void RefreshDone()
    {
        _doneLabel.Text =
            $"QuickReply was installed to:\n    {_choices.InstallPath}\n\n" +
            $"Press {_choices.Hotkey} anywhere in Windows to open the picker.\n\n" +
            (_choices.RunOnStartup
                ? "QuickReply will start automatically the next time you sign in."
                : "QuickReply will not auto-start. Open it from the install folder whenever you need it.");
    }

    private async Task RunInstallAsync()
    {
        _installer.ProgressChanged += pct =>
        {
            if (InvokeRequired) BeginInvoke(new Action(() => _installProgress.Value = Math.Clamp(pct, 0, 100)));
            else _installProgress.Value = Math.Clamp(pct, 0, 100);
        };
        _installer.StatusChanged += status =>
        {
            if (InvokeRequired) BeginInvoke(new Action(() => _installStatusLabel.Text = status));
            else _installStatusLabel.Text = status;
        };

        try
        {
            await _installer.InstallAsync(_choices).ConfigureAwait(true);
            ShowPage(IndexOf("Done"));
        }
        catch (Exception ex)
        {
            _installErrorLabel.Visible = true;
            _installErrorLabel.Text =
                "Install failed.\n\n" + ex.Message + "\n\n" +
                "Click Cancel to close the wizard and try again, or check your network connection.";
            _backButton.Visible = false;
            _nextButton.Visible = false;
            _cancelButton.Visible = true;
            _cancelButton.Text = "Close";
        }
    }

    private void FinishWizard()
    {
        if (_launchAfterInstall.Checked)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(_choices.InstallPath, "QuickReply.exe"),
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Could not launch QuickReply: " + ex.Message,
                    "QuickReply Setup",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        Close();
    }

    private void RequestCancel()
    {
        if (_currentPageIndex >= IndexOf("Installing")) { Close(); return; }
        var result = MessageBox.Show(this,
            "Cancel setup? QuickReply has not been installed yet.",
            "QuickReply Setup",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes) Close();
    }

    // --- Helpers -----------------------------------------------------------

    private int IndexOf(string pageName) => _pages.FindIndex(p => p.Name == pageName);

    private void AddPageHeader(Panel panel, string title, string body)
    {
        var titleLabel = new Label
        {
            Text = title,
            Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 24)
        };
        var bodyLabel = new Label
        {
            Text = body,
            Font = Theme.BodyLg(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(2, 64)
        };
        panel.Controls.Add(titleLabel);
        panel.Controls.Add(bodyLabel);
    }

    private RadioButton MakeRadio(string text, Point location) => new()
    {
        Text = text,
        AutoSize = true,
        Location = location,
        Font = Theme.BodyLg(),
        ForeColor = Theme.Text,
        BackColor = Theme.Bg,
        FlatStyle = FlatStyle.Standard
    };

    private static bool IsHotkeyShapeValid(string hk)
    {
        if (string.IsNullOrWhiteSpace(hk)) return false;
        var parts = hk.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false;
        var hasModifier = false;
        var hasKey = false;
        foreach (var p in parts)
        {
            var n = p.ToLowerInvariant();
            if (n is "ctrl" or "control" or "alt" or "shift" or "win" or "windows") hasModifier = true;
            else hasKey = true;
        }
        return hasModifier && hasKey;
    }

    private void ValidateHotkey(string hk)
    {
        if (IsHotkeyShapeValid(hk))
        {
            _hotkeyValidationLabel.Text = $"Will register: {hk.Trim()}";
            _hotkeyValidationLabel.ForeColor = Theme.Success;
        }
        else
        {
            _hotkeyValidationLabel.Text = "Need at least one modifier (Ctrl/Alt/Shift/Win) and a key.";
            _hotkeyValidationLabel.ForeColor = Theme.Danger;
        }
    }

    private void Warn(string message) =>
        MessageBox.Show(this, message, "QuickReply Setup",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private void DrawBorder(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Theme.BorderHi, 1f);
        g.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        using var divider = new Pen(Theme.Border, 1f);
        g.DrawLine(divider, 24, 84, ClientSize.Width - 24, 84);
        g.DrawLine(divider, 24, ClientSize.Height - 78, ClientSize.Width - 24, ClientSize.Height - 78);
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

    // Lets us drag the borderless form by its header area.
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
                var screenPt = new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF));
                var clientPt = PointToClient(screenPt);
                if (clientPt.Y < 84 && clientPt.X < ClientSize.Width - 40)
                {
                    m.Result = (IntPtr)HTCAPTION;
                }
            }
            return;
        }
        base.WndProc(ref m);
    }

    private record Page(Panel Container, string StepLabel, string Name);
}
