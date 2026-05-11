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
