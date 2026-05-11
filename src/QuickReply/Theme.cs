using System.Drawing.Drawing2D;

namespace QuickReply;

internal static class Theme
{
    // Palette: cool slate background, indigo accent, gentle contrast.
    public static readonly Color Bg            = Color.FromArgb(20, 22, 27);
    public static readonly Color BgRaised      = Color.FromArgb(26, 29, 35);
    public static readonly Color Surface       = Color.FromArgb(33, 37, 45);
    public static readonly Color SurfaceHi     = Color.FromArgb(43, 48, 58);
    public static readonly Color SurfaceHover  = Color.FromArgb(52, 58, 70);
    public static readonly Color Border        = Color.FromArgb(45, 50, 60);
    public static readonly Color BorderHi      = Color.FromArgb(72, 80, 96);
    public static readonly Color Accent        = Color.FromArgb(99, 102, 241);
    public static readonly Color AccentHi      = Color.FromArgb(129, 140, 248);
    public static readonly Color AccentSoft    = Color.FromArgb(165, 175, 245);
    public static readonly Color AccentBg      = Color.FromArgb(40, 41, 80);
    public static readonly Color Text          = Color.FromArgb(229, 231, 235);
    public static readonly Color TextMuted     = Color.FromArgb(156, 163, 175);
    public static readonly Color TextDim       = Color.FromArgb(107, 114, 128);
    public static readonly Color Success       = Color.FromArgb(74, 222, 128);
    public static readonly Color Danger        = Color.FromArgb(248, 113, 113);

    public static Font H1()        => new("Segoe UI Semibold", 15.5f, FontStyle.Bold);
    public static Font Subtitle()  => new("Segoe UI", 9f, FontStyle.Regular);
    public static Font BodyLg()    => new("Segoe UI", 11.5f, FontStyle.Regular);
    public static Font Preview()   => new("Segoe UI", 10f, FontStyle.Regular);
    public static Font Caps()      => new("Segoe UI Semibold", 7.75f, FontStyle.Bold);
    public static Font Status()    => new("Segoe UI", 8.75f, FontStyle.Italic);
    public static Font ChipCode()  => new("Segoe UI Semibold", 9.25f, FontStyle.Bold);
    public static Font ChipLabel() => new("Segoe UI", 9f, FontStyle.Regular);
    public static Font Button()    => new("Segoe UI Semibold", 9.25f, FontStyle.Bold);
    public static Font Pill()      => new("Segoe UI Semibold", 8.25f, FontStyle.Bold);
    public static Font Chevron()   => new("Segoe UI", 18f, FontStyle.Bold);

    public static GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>Rounded-corner panel used as a card / input wrapper.</summary>
internal class CardPanel : Panel
{
    public int CornerRadius { get; set; } = 10;
    public Color FillColor { get; set; } = Theme.Surface;
    public Color BorderColor { get; set; } = Theme.Border;
    public Color BorderColorFocused { get; set; } = Theme.Accent;
    public bool IsFocused { get; set; }

    public CardPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Bg;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = Theme.RoundedRect(rect, CornerRadius);
        using (var fill = new SolidBrush(FillColor))
            g.FillPath(fill, path);
        using (var pen = new Pen(IsFocused ? BorderColorFocused : BorderColor, IsFocused ? 1.4f : 1f))
            g.DrawPath(pen, path);
    }
}

/// <summary>Quick-pick chip button: monospace-feel code + descriptive label.</summary>
internal class ChipButton : Control
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    private bool _hover;
    private bool _pressed;

    public ChipButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Bg;
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); }
        base.OnMouseDown(e);
    }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = Theme.RoundedRect(rect, 8);

        var fillColor = _pressed ? Theme.SurfaceHover
                       : _hover ? Theme.SurfaceHi
                       : Theme.Surface;
        var borderColor = _hover ? Theme.BorderHi : Theme.Border;

        using (var fill = new SolidBrush(fillColor))
            g.FillPath(fill, path);
        using (var pen = new Pen(borderColor, 1f))
            g.DrawPath(pen, path);

        const int padX = 12;
        using var codeFont = Theme.ChipCode();
        using var labelFont = Theme.ChipLabel();

        var codeSize = TextRenderer.MeasureText(g, Code, codeFont, Size.Empty, TextFormatFlags.NoPadding);
        var codeY = (Height - codeSize.Height) / 2;
        TextRenderer.DrawText(g, Code, codeFont, new Point(padX, codeY),
            _hover ? Theme.AccentHi : Theme.AccentSoft, TextFormatFlags.NoPadding);

        var labelX = padX + codeSize.Width + 10;
        var labelSize = TextRenderer.MeasureText(g, Label, labelFont, Size.Empty, TextFormatFlags.NoPadding);
        var labelY = (Height - labelSize.Height) / 2;
        TextRenderer.DrawText(g, Label, labelFont, new Point(labelX, labelY),
            Theme.Text, TextFormatFlags.NoPadding);
    }
}

internal enum ActionButtonStyle { Primary, Secondary, Ghost }

/// <summary>Footer action button. Primary = filled accent. Secondary = surface. Ghost = outline only.</summary>
internal class ActionButton : Control
{
    public ActionButtonStyle Style { get; set; } = ActionButtonStyle.Secondary;
    private bool _hover;
    private bool _pressed;

    public ActionButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Bg;
        Cursor = Cursors.Hand;
        Height = 34;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); }
        base.OnMouseDown(e);
    }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = Theme.RoundedRect(rect, 8);

        Color fillColor, borderColor, textColor;
        switch (Style)
        {
            case ActionButtonStyle.Primary:
                if (!Enabled)
                {
                    fillColor = Color.FromArgb(50, 53, 75);
                    borderColor = fillColor;
                    textColor = Color.FromArgb(160, 165, 180);
                }
                else
                {
                    fillColor = _hover && !_pressed ? Theme.AccentHi : Theme.Accent;
                    borderColor = fillColor;
                    textColor = Color.White;
                }
                break;
            case ActionButtonStyle.Secondary:
                fillColor = _pressed ? Theme.SurfaceHover
                          : _hover ? Theme.SurfaceHi
                          : Theme.Surface;
                borderColor = _hover ? Theme.BorderHi : Theme.Border;
                textColor = Enabled ? Theme.Text : Theme.TextDim;
                break;
            default: // Ghost
                fillColor = _hover ? Theme.BgRaised : Theme.Bg;
                borderColor = _hover ? Theme.BorderHi : Theme.Border;
                textColor = Enabled ? Theme.TextMuted : Theme.TextDim;
                break;
        }

        using (var fill = new SolidBrush(fillColor))
            g.FillPath(fill, path);
        using (var pen = new Pen(borderColor, 1f))
            g.DrawPath(pen, path);

        using var font = Theme.Button();
        TextRenderer.DrawText(g, Text, font, ClientRectangle, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

/// <summary>Pill-shaped accent badge used to display the hotkey in the header.</summary>
internal class HotkeyPill : Control
{
    public HotkeyPill()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Bg;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = Theme.RoundedRect(rect, Height / 2f);
        using (var fill = new SolidBrush(Theme.AccentBg))
            g.FillPath(fill, path);
        using (var pen = new Pen(Theme.Accent, 1f))
            g.DrawPath(pen, path);

        using var font = Theme.Pill();
        TextRenderer.DrawText(g, Text, font, ClientRectangle, Theme.AccentSoft,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
