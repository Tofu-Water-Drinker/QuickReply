using System.Collections.Specialized;

namespace QuickReply;

/// <summary>
/// Clipboard helpers. The non-obvious thing this class does is take a real
/// snapshot of the clipboard (across HTML, RTF, plain text, image, and file
/// drop formats) before QuickReply overwrites it, so the original payload
/// can be restored after paste. Earlier versions only saved plain text,
/// which silently destroyed images, file lists, and rich content.
/// </summary>
public static class ClipboardService
{
    /// <summary>
    /// Captures a multi-format snapshot of whatever is currently on the
    /// clipboard. Returns <c>null</c> if the clipboard is empty, unreadable,
    /// or contains nothing in a format we know how to put back.
    /// </summary>
    public static ClipboardSnapshot? CaptureSnapshot()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (!Clipboard.ContainsData(DataFormats.UnicodeText)
                    && !Clipboard.ContainsText()
                    && !Clipboard.ContainsImage()
                    && !Clipboard.ContainsFileDropList()
                    && !Clipboard.ContainsData(DataFormats.Html)
                    && !Clipboard.ContainsData(DataFormats.Rtf))
                {
                    return null;
                }

                var snap = new ClipboardSnapshot();

                // Text formats: prefer the richer formats when present.
                try { if (Clipboard.ContainsData(DataFormats.Html))
                        snap.Html = Clipboard.GetData(DataFormats.Html) as string; } catch { }
                try { if (Clipboard.ContainsData(DataFormats.Rtf))
                        snap.Rtf = Clipboard.GetData(DataFormats.Rtf) as string; } catch { }
                try { if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                        snap.UnicodeText = Clipboard.GetText(TextDataFormat.UnicodeText); } catch { }
                try { if (Clipboard.ContainsText(TextDataFormat.Text))
                        snap.PlainText = Clipboard.GetText(TextDataFormat.Text); } catch { }

                // Image (lossy: keeps a managed bitmap copy).
                try
                {
                    if (Clipboard.ContainsImage())
                    {
                        using var src = Clipboard.GetImage();
                        if (src != null) snap.Image = new Bitmap(src);
                    }
                }
                catch { /* image format not understood; skip */ }

                // File drop list.
                try
                {
                    if (Clipboard.ContainsFileDropList())
                    {
                        var files = Clipboard.GetFileDropList();
                        if (files != null && files.Count > 0)
                        {
                            snap.FileDropList = new StringCollection();
                            foreach (var f in files) snap.FileDropList.Add(f);
                        }
                    }
                }
                catch { /* skip */ }

                return snap.IsEmpty ? null : snap;
            }
            catch
            {
                Thread.Sleep(40);
            }
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

    /// <summary>
    /// Restores a previously captured snapshot. Best-effort: walks every
    /// format we captured and puts them back on a single new
    /// <see cref="DataObject"/> so a downstream paste sees what was there
    /// before, not just the plain-text reduction of it.
    /// </summary>
    public static void Restore(ClipboardSnapshot? snapshot)
    {
        if (snapshot == null || snapshot.IsEmpty) return;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                var data = new DataObject();
                if (snapshot.Html != null)        data.SetData(DataFormats.Html, snapshot.Html);
                if (snapshot.Rtf != null)         data.SetData(DataFormats.Rtf, snapshot.Rtf);
                if (snapshot.UnicodeText != null) data.SetText(snapshot.UnicodeText, TextDataFormat.UnicodeText);
                else if (snapshot.PlainText != null) data.SetText(snapshot.PlainText, TextDataFormat.Text);
                if (snapshot.Image != null)       data.SetImage(snapshot.Image);
                if (snapshot.FileDropList != null && snapshot.FileDropList.Count > 0)
                    data.SetFileDropList(snapshot.FileDropList);

                Clipboard.SetDataObject(data, copy: true);
                return;
            }
            catch
            {
                Thread.Sleep(40);
            }
        }
    }
}

/// <summary>
/// A best-effort snapshot of clipboard contents across the formats QuickReply
/// can faithfully restore. Anything we cannot capture (custom app formats,
/// virtual file lists, OLE embeddings) is intentionally dropped.
/// </summary>
public sealed class ClipboardSnapshot : IDisposable
{
    public string? Html { get; set; }
    public string? Rtf { get; set; }
    public string? UnicodeText { get; set; }
    public string? PlainText { get; set; }
    public Image? Image { get; set; }
    public StringCollection? FileDropList { get; set; }

    public bool IsEmpty =>
        Html == null && Rtf == null
        && UnicodeText == null && PlainText == null
        && Image == null
        && (FileDropList == null || FileDropList.Count == 0);

    public void Dispose()
    {
        try { Image?.Dispose(); } catch { /* ignore */ }
        Image = null;
    }
}
