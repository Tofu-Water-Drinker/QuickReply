using System.Drawing.Drawing2D;

namespace QuickReply;

/// <summary>
/// A short five-page tour shown on first launch (and on demand from the
/// tray menu). Covers: welcome, the hotkey, what ships with the app,
/// managing things from the tray, and "you're ready". Every page has a
/// Skip Tutorial button so a returning user is never trapped.
/// </summary>
public class TutorialForm : Form
{
    private readonly List<Page> _pages = new();
    private int _currentPageIndex;

    private Label _titleLabel = null!;
    private Label _stepLabel = null!;
    private ActionButton _backButton = null!;
    private ActionButton _nextButton = null!;
    private ActionButton _skipButton = null!;

    public TutorialForm()
    {
        InitializeUi();
        ShowPage(0);
    }

    private void InitializeUi()
    {
        SuspendLayout();

        Text = "QuickReply Tutorial";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.75f);
        ClientSize = new Size(720, 520);
        DoubleBuffered = true;
        KeyPreview = true;

        BuildChrome();
        BuildPages();

        Paint += DrawChrome;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; FinishTutorial(); }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                GoNext();
            }
            else if (e.KeyCode == Keys.Left)
            {
                e.SuppressKeyPress = true;
                GoBack();
            }
        };

        ResumeLayout(false);
        PerformLayout();
    }

    private void BuildChrome()
    {
        _titleLabel = new Label
        {
            Text = "Welcome to QuickReply",
            Font = Theme.H1(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(24, 20)
        };
        _stepLabel = new Label
        {
            Text = "Step 1 of 5",
            Font = Theme.Subtitle(),
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(26, 50)
        };

        _skipButton = new ActionButton
        {
            Text = "Skip tutorial",
            Style = ActionButtonStyle.Ghost,
            Size = new Size(108, 34),
            Location = new Point(24, ClientSize.Height - 58),
            BackColor = Theme.Bg
        };
        _skipButton.Click += (_, _) => FinishTutorial();

        _backButton = new ActionButton
        {
            Text = "< Back",
            Style = ActionButtonStyle.Ghost,
            Size = new Size(80, 34),
            Location = new Point(ClientSize.Width - 24 - 200, ClientSize.Height - 58),
            BackColor = Theme.Bg
        };
        _backButton.Click += (_, _) => GoBack();

        _nextButton = new ActionButton
        {
            Text = "Next >",
            Style = ActionButtonStyle.Primary,
            Size = new Size(108, 34),
            Location = new Point(ClientSize.Width - 24 - 108, ClientSize.Height - 58),
            BackColor = Theme.Bg
        };
        _nextButton.Click += (_, _) => GoNext();

        Controls.Add(_titleLabel);
        Controls.Add(_stepLabel);
        Controls.Add(_skipButton);
        Controls.Add(_backButton);
        Controls.Add(_nextButton);
    }

    private void BuildPages()
    {
        _pages.Add(BuildWelcomePage());
        _pages.Add(BuildHotkeyPage());
        _pages.Add(BuildIncludedPage());
        _pages.Add(BuildManagePage());
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
        var heading = new Label
        {
            Text = "Welcome.",
            Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 24)
        };
        var body = new Label
        {
            Text =
                "QuickReply lives in your system tray and helps you paste consistent\n" +
                "service desk replies without retyping them every time.\n\n" +
                "This short tour covers the four things worth knowing in about\n" +
                "60 seconds:\n\n" +
                "    1. How to open the picker with your hotkey\n" +
                "    2. What ships with the app (snippets, signatures, aliases)\n" +
                "    3. How to manage everything from the tray menu\n" +
                "    4. Where to go next\n\n" +
                "Click Next to begin, or Skip tutorial to dive in. You can replay\n" +
                "this tour any time from the tray menu.",
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 76)
        };
        panel.Controls.Add(heading);
        panel.Controls.Add(body);
        return new Page(panel, "Step 1 of 5", "Welcome to QuickReply");
    }

    private Page BuildHotkeyPage()
    {
        var panel = new Panel();
        var heading = new Label
        {
            Text = "Press your hotkey, anywhere.",
            Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 24)
        };

        var pill = new HotkeyPill
        {
            Text = "Ctrl + Alt + ;",
            Size = new Size(140, 32),
            Location = new Point(0, 80),
            BackColor = Theme.Bg
        };

        var body = new Label
        {
            Text =
                "Press the hotkey anywhere in Windows. A small dark picker opens,\n" +
                "centered on the screen your cursor is on, focused on the code input.\n\n" +
                "In the picker:\n\n" +
                "    •  Type a code like fu, vm, or close\n" +
                "    •  Or click one of the 12 quick-pick chips at the bottom\n" +
                "    •  The preview updates live as you type\n" +
                "    •  Press Enter (or click Paste) to paste into the window you came from\n" +
                "    •  Click Copy Only for picky fields that reject programmatic paste\n" +
                "    •  Press Esc, or press the hotkey again, to close the picker\n\n" +
                "QuickReply remembers exactly which text box you were typing in, and\n" +
                "puts the cursor back there before pasting.",
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 124)
        };
        panel.Controls.Add(heading);
        panel.Controls.Add(pill);
        panel.Controls.Add(body);
        return new Page(panel, "Step 2 of 5", "Press your hotkey, anywhere.");
    }

    private Page BuildIncludedPage()
    {
        var panel = new Panel();
        var heading = new Label
        {
            Text = "35+ snippets, with variation.",
            Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 24)
        };
        var body = new Label
        {
            Text =
                "QuickReply ships with the snippets a service desk tech actually uses:\n\n" +
                "    fu          Follow up on a ticket\n" +
                "    vm          Left a voicemail\n" +
                "    close       Resolve and close a ticket\n" +
                "    rbt         Ask the user to reboot\n" +
                "    ts, note    Multi-line ticket templates\n" +
                "    esc         Escalation summary template\n" +
                "    vendorcase  Vendor case opened, waiting on response\n" +
                "    sig         Your rich-text signature (HTML, images, styling)\n\n" +
                "Variants: most conversational codes have 8 different ways to say the\n" +
                "same thing. Type fu on two tickets and customers see two different\n" +
                "sentences, not the same paragraph copy-pasted.\n\n" +
                "Aliases: rbt, reboot, and restart all paste the reboot reply. Use\n" +
                "whichever shorthand sticks in your head.",
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 76)
        };
        panel.Controls.Add(heading);
        panel.Controls.Add(body);
        return new Page(panel, "Step 3 of 5", "35+ snippets, with variation.");
    }

    private Page BuildManagePage()
    {
        var panel = new Panel();
        var heading = new Label
        {
            Text = "Manage everything from the tray.",
            Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 24)
        };
        var body = new Label
        {
            Text =
                "Right-click the QuickReply icon in your system tray for the full menu.\n" +
                "Everything is editable in a GUI; you never need to open a JSON file.\n\n" +
                "    •  Manage Snippets...   list, edit, delete, and add any snippet\n" +
                "    •  Add Snippet...       focused editor for a single new snippet\n" +
                "    •  Edit Signature...    rich HTML signature editor with preview\n" +
                "    •  Settings...          hotkey, paste delays, and app behavior\n" +
                "    •  Show Tutorial...     replay this tour any time\n" +
                "    •  Check for Updates... see if a newer release is on GitHub\n\n" +
                "All changes save automatically. The picker re-reads on the fly so you\n" +
                "do not need to restart the app.",
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 76)
        };
        panel.Controls.Add(heading);
        panel.Controls.Add(body);
        return new Page(panel, "Step 4 of 5", "Manage everything from the tray.");
    }

    private Page BuildDonePage()
    {
        var panel = new Panel();
        var heading = new Label
        {
            Text = "You're ready.",
            Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
            ForeColor = Theme.Success,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 24)
        };
        var body = new Label
        {
            Text =
                "That's everything. A few things to try right now:\n\n" +
                "    •  Close this window\n" +
                "    •  Click into any text field anywhere in Windows\n" +
                "    •  Press Ctrl + Alt + ;\n" +
                "    •  Type fu and press Enter\n\n" +
                "QuickReply lives in the tray. Right-click it for the menu, or just\n" +
                "press your hotkey whenever you need a snippet.",
            Font = Theme.BodyLg(),
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = true,
            Location = new Point(0, 76)
        };
        panel.Controls.Add(heading);
        panel.Controls.Add(body);
        return new Page(panel, "Step 5 of 5", "You're ready.");
    }

    // ── Navigation ─────────────────────────────────────────────────────

    private void ShowPage(int index)
    {
        for (var i = 0; i < _pages.Count; i++) _pages[i].Container.Visible = (i == index);
        _currentPageIndex = index;
        _stepLabel.Text = _pages[index].StepLabel;
        _titleLabel.Text = _pages[index].Title;

        _backButton.Enabled = index > 0;
        _nextButton.Text = (index == _pages.Count - 1) ? "Finish" : "Next >";
    }

    private void GoNext()
    {
        if (_currentPageIndex == _pages.Count - 1)
        {
            FinishTutorial();
            return;
        }
        ShowPage(_currentPageIndex + 1);
    }

    private void GoBack()
    {
        if (_currentPageIndex == 0) return;
        ShowPage(_currentPageIndex - 1);
    }

    private void FinishTutorial()
    {
        DialogResult = DialogResult.OK;
        Close();
    }

    // ── Chrome ─────────────────────────────────────────────────────────

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

    private record Page(Panel Container, string StepLabel, string Title);
}
