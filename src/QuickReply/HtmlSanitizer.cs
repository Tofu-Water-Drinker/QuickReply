using System.Text.RegularExpressions;

namespace QuickReply;

/// <summary>
/// A deliberately small, deliberately strict sanitizer for email-style
/// signature HTML. This is not a general-purpose XSS filter, and it is not
/// trying to be one. Its job is to make a "safe enough" version of the user's
/// signature so we never paste a &lt;script&gt; or a <c>javascript:</c> link
/// into a ticket or email.
///
/// Strategy: drop dangerous elements wholesale (script, style, iframe, object,
/// embed, link, meta, base), strip on* event-handler attributes, strip
/// <c>javascript:</c> URLs in href/src/action. Everything else is kept as-is.
/// </summary>
public static class HtmlSanitizer
{
    // Elements we delete with their entire subtree.
    private static readonly string[] DangerousElements =
    {
        "script", "style", "iframe", "object", "embed", "applet",
        "link", "meta", "base", "form", "frame", "frameset"
    };

    // Attributes we always strip even on otherwise allowed elements.
    // (Anything starting with "on" is treated as an event handler.)
    private static readonly string[] StrippedAttributes =
    {
        "srcdoc", "formaction", "xmlns", "background"
    };

    private static readonly Regex CommentRegex = new(
        @"<!--.*?-->",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex EventAttrRegex = new(
        @"\son[a-z]+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex JavascriptUrlAttrRegex = new(
        @"\s(href|src|action|formaction|xlink:href)\s*=\s*(""\s*javascript:[^""]*""|'\s*javascript:[^']*'|javascript:[^\s>]*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DataHtmlAttrRegex = new(
        // data:text/html and data: with no MIME both have script-execution risk
        // in some HTML contexts. data:image/* stays allowed because that is how
        // we embed signature logos.
        @"\s(href|src)\s*=\s*(""\s*data:(?!image/)[^""]*""|'\s*data:(?!image/)[^']*'|data:(?!image/)[^\s>]*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Returns a sanitized copy of <paramref name="html"/>. Safe to call on
    /// already-sanitized input (idempotent).
    /// </summary>
    public static string Sanitize(string html)
    {
        if (string.IsNullOrEmpty(html)) return html;

        // 1. Strip HTML comments. They can hide payloads from quick reads and
        //    serve no purpose in a signature.
        var result = CommentRegex.Replace(html, string.Empty);

        // 2. Remove dangerous elements and their full subtree. Done greedily
        //    against the lowercased name, case-insensitive.
        foreach (var tag in DangerousElements)
        {
            var blockRegex = new Regex(
                $@"<\s*{tag}\b[^>]*>.*?<\s*/\s*{tag}\s*>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            result = blockRegex.Replace(result, string.Empty);

            // Self-closing or unmatched-open cases (e.g. <link>, <meta />).
            var voidRegex = new Regex(
                $@"<\s*{tag}\b[^>]*/?>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            result = voidRegex.Replace(result, string.Empty);
        }

        // 3. Strip event-handler attributes (onclick, onmouseover, on*).
        result = EventAttrRegex.Replace(result, string.Empty);

        // 4. Strip explicitly named bad attributes.
        foreach (var attr in StrippedAttributes)
        {
            var attrRegex = new Regex(
                $@"\s{attr}\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)",
                RegexOptions.IgnoreCase);
            result = attrRegex.Replace(result, string.Empty);
        }

        // 5. Neutralize javascript: URLs in href/src/action.
        result = JavascriptUrlAttrRegex.Replace(result, " $1=\"#\"");

        // 6. Block data: URLs except data:image/*.
        result = DataHtmlAttrRegex.Replace(result, " $1=\"#\"");

        return result;
    }

    /// <summary>
    /// Returns a short list of human-readable warnings about
    /// <paramref name="html"/>: any pattern <see cref="Sanitize"/> would
    /// strip. The signature editor uses this to show a visible "this looks
    /// risky" banner before the user pastes it anywhere.
    /// </summary>
    public static IReadOnlyList<string> Inspect(string html)
    {
        var warnings = new List<string>();
        if (string.IsNullOrEmpty(html)) return warnings;

        if (Regex.IsMatch(html, @"<\s*script\b", RegexOptions.IgnoreCase))
            warnings.Add("Contains <script>. Will be removed in safe mode.");

        if (Regex.IsMatch(html, @"<\s*(iframe|object|embed|frame|frameset|applet)\b", RegexOptions.IgnoreCase))
            warnings.Add("Contains an embedded frame or object tag. Will be removed in safe mode.");

        if (Regex.IsMatch(html, @"<\s*(link|meta|base)\b", RegexOptions.IgnoreCase))
            warnings.Add("Contains <link>, <meta>, or <base>. Will be removed in safe mode.");

        if (EventAttrRegex.IsMatch(html))
            warnings.Add("Has event-handler attributes (onclick, onload, etc.). Will be stripped in safe mode.");

        if (JavascriptUrlAttrRegex.IsMatch(html))
            warnings.Add("Has javascript: URLs in href/src. Will be neutralised in safe mode.");

        if (DataHtmlAttrRegex.IsMatch(html))
            warnings.Add("Has non-image data: URLs. Will be neutralised in safe mode.");

        // External HTTP(S) image references are not stripped (legitimate
        // signatures use them), but flag them so the user can decide.
        if (Regex.IsMatch(html, @"<\s*img\b[^>]+src\s*=\s*[""']?https?:", RegexOptions.IgnoreCase))
            warnings.Add("References remote images over http(s). These can be tracking pixels; consider embedding via Insert image instead.");

        return warnings;
    }
}
