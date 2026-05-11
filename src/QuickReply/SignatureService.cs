using System.Net;
using System.Text.RegularExpressions;

namespace QuickReply;

/// <summary>
/// Owns the user's email-style signature: a single HTML file kept next to
/// the exe. The signature is intentionally separate from <see cref="SnippetService"/>
/// because it round-trips as rich content (HTML + plain text), can embed
/// images as data URIs, and lives in one place (not as one entry among many).
/// </summary>
public class SignatureService
{
    private readonly SettingsService? _settings;

    public string FilePath { get; }

    public event EventHandler? Changed;

    public SignatureService(string filePath, SettingsService? settings = null)
    {
        FilePath = filePath;
        _settings = settings;
    }

    private bool SafeMode => _settings?.Current.SafeSignatureMode ?? true;

    public bool LoadOrCreate()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                File.WriteAllText(FilePath, DefaultTemplateHtml);
            }
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not initialise signature.html.\n\n{ex.Message}",
                "QuickReply",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
    }

    /// <summary>
    /// Returns the signature HTML as stored. Use <see cref="GetHtmlForPaste"/>
    /// when about to put HTML on the clipboard, since that path applies the
    /// sanitizer when safe mode is on.
    /// </summary>
    public string GetHtml()
    {
        try
        {
            return File.Exists(FilePath)
                ? File.ReadAllText(FilePath)
                : DefaultTemplateHtml;
        }
        catch
        {
            return DefaultTemplateHtml;
        }
    }

    /// <summary>
    /// Returns the signature HTML run through the sanitizer if safe mode is
    /// on, or the raw HTML otherwise. This is what the paste path should use.
    /// </summary>
    public string GetHtmlForPaste()
    {
        var raw = GetHtml();
        return SafeMode ? HtmlSanitizer.Sanitize(raw) : raw;
    }

    public string GetPlainText() => HtmlToPlainText(GetHtml());

    public bool Save(string html)
    {
        try
        {
            File.WriteAllText(FilePath, html ?? string.Empty);
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not save signature.\n\n{ex.Message}",
                "QuickReply",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
    }

    public bool ResetToDefault() => Save(DefaultTemplateHtml);

    // ── HTML to plain text ───────────────────────────────────────────────

    private static readonly Regex BrRegex   = new(@"<br\s*/?>",       RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PRegex    = new(@"</p\s*>",         RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DivRegex  = new(@"</div\s*>",       RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TagRegex  = new(@"<[^>]+>",         RegexOptions.Compiled);
    private static readonly Regex BlankLineRegex = new(@"(\r?\n){3,}",RegexOptions.Compiled);

    public static string HtmlToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var text = html;
        text = BrRegex.Replace(text, "\n");
        text = PRegex.Replace(text, "\n\n");
        text = DivRegex.Replace(text, "\n");
        text = TagRegex.Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = BlankLineRegex.Replace(text, "\n\n");
        return text.Trim();
    }

    // ── Default template ─────────────────────────────────────────────────

    public const string DefaultTemplateHtml =
        "<div style=\"font-family: 'Segoe UI', Arial, sans-serif; font-size: 11pt; color: #1f2937; line-height: 1.45;\">\r\n" +
        "  <p style=\"margin: 0;\">Thanks,</p>\r\n" +
        "  <p style=\"margin: 12px 0 4px 0;\"><strong style=\"font-size: 13pt;\">Your Name</strong></p>\r\n" +
        "  <p style=\"margin: 0; color: #6b7280;\">Service Desk &middot; Your Company</p>\r\n" +
        "  <p style=\"margin: 0; color: #6b7280;\">\r\n" +
        "    <a href=\"mailto:you@example.com\" style=\"color: #6366f1; text-decoration: none;\">you@example.com</a>\r\n" +
        "    &middot; (555) 123-4567\r\n" +
        "  </p>\r\n" +
        "</div>\r\n";

    public const string MinimalTemplateHtml =
        "<div style=\"font-family: 'Segoe UI', Arial, sans-serif; font-size: 11pt;\">\r\n" +
        "  <p style=\"margin: 0;\">Thanks,</p>\r\n" +
        "  <p style=\"margin: 6px 0 0 0;\"><strong>Your Name</strong></p>\r\n" +
        "</div>\r\n";

    public const string WithLogoTemplateHtml =
        "<div style=\"font-family: 'Segoe UI', Arial, sans-serif; font-size: 11pt; color: #1f2937; line-height: 1.45;\">\r\n" +
        "  <p style=\"margin: 0 0 10px 0;\">Thanks,</p>\r\n" +
        "  <table cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr>\r\n" +
        "    <td style=\"padding-right: 14px; vertical-align: top;\">\r\n" +
        "      <!-- Replace this img tag with your logo (use the Insert Image button). -->\r\n" +
        "      <div style=\"width: 64px; height: 64px; background: #6366f1; border-radius: 8px;\"></div>\r\n" +
        "    </td>\r\n" +
        "    <td style=\"vertical-align: top;\">\r\n" +
        "      <div style=\"font-size: 13pt;\"><strong>Your Name</strong></div>\r\n" +
        "      <div style=\"color: #6b7280;\">Service Desk &middot; Your Company</div>\r\n" +
        "      <div style=\"color: #6b7280;\">\r\n" +
        "        <a href=\"mailto:you@example.com\" style=\"color: #6366f1; text-decoration: none;\">you@example.com</a>\r\n" +
        "        &middot; (555) 123-4567\r\n" +
        "      </div>\r\n" +
        "    </td>\r\n" +
        "  </tr></table>\r\n" +
        "</div>\r\n";
}
