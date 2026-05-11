namespace QuickReply;

public static class ClipboardService
{
    public static string? SaveText()
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                return Clipboard.GetText();
            }
        }
        catch
        {
            // Clipboard can be locked by another app or contain non-text data.
        }
        return null;
    }

    public static bool SetText(string text)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return true;
            }
            catch
            {
                Thread.Sleep(40);
            }
        }
        return false;
    }

    /// <summary>
    /// Puts both HTML and plain text on the clipboard. Apps that understand
    /// rich paste (Outlook, Gmail web, Teams, modern ticket systems) pick up
    /// the HTML; everything else falls back to <paramref name="plainText"/>.
    /// .NET wraps the HTML in the required CF_HTML header automatically when
    /// using <see cref="TextDataFormat.Html"/>.
    /// </summary>
    public static bool SetRichText(string html, string plainText)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                var data = new DataObject();
                data.SetText(html, TextDataFormat.Html);
                data.SetText(plainText, TextDataFormat.UnicodeText);
                Clipboard.SetDataObject(data, copy: true);
                return true;
            }
            catch
            {
                Thread.Sleep(40);
            }
        }
        return false;
    }

    public static void RestoreText(string? previous)
    {
        if (previous == null) return;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (string.IsNullOrEmpty(previous))
                {
                    Clipboard.Clear();
                }
                else
                {
                    Clipboard.SetText(previous);
                }
                return;
            }
            catch
            {
                Thread.Sleep(40);
            }
        }
    }
}
