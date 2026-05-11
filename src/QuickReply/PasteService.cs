using QuickReply.Models;

namespace QuickReply;

public class PasteService
{
    private readonly SettingsService _settingsService;

    public PasteService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public static IntPtr CaptureForegroundWindow() => FocusHelper.CurrentForegroundWindow();

    /// <summary>
    /// Puts <paramref name="text"/> on the clipboard. If a previous foreground window
    /// is supplied and AutoPaste is enabled, restores focus and sends Ctrl+V.
    /// </summary>
    public PasteResult PasteOrCopy(string text, IntPtr previousWindow, IntPtr previousFocusedControl = default) =>
        PasteOrCopyInternal(
            putOnClipboard: () => ClipboardService.SetText(text),
            previousWindow: previousWindow,
            previousFocusedControl: previousFocusedControl);

    /// <summary>
    /// Same flow as <see cref="PasteOrCopy"/> but puts BOTH HTML and plain text
    /// on the clipboard so rich-text-aware apps render the styling and images,
    /// while plain-text-only fields get a clean fallback.
    /// </summary>
    public PasteResult PasteOrCopyRich(string html, string plainText, IntPtr previousWindow, IntPtr previousFocusedControl = default) =>
        PasteOrCopyInternal(
            putOnClipboard: () => ClipboardService.SetRichText(html, plainText),
            previousWindow: previousWindow,
            previousFocusedControl: previousFocusedControl);

    private PasteResult PasteOrCopyInternal(Func<bool> putOnClipboard, IntPtr previousWindow, IntPtr previousFocusedControl)
    {
        var settings = _settingsService.Current;
        var previousClipboard = settings.RestoreClipboardAfterPaste
            ? ClipboardService.SaveText()
            : null;

        if (!putOnClipboard())
        {
            return new PasteResult(false, false, "Clipboard is unavailable.");
        }

        if (!settings.AutoPaste || previousWindow == IntPtr.Zero)
        {
            return new PasteResult(true, false, settings.AutoPaste
                ? "Copied (no target window)."
                : "Copied. AutoPaste is disabled.");
        }

        // Give Windows a beat to finish hiding the picker and reassign
        // foreground before we try to take it. Without this, the focus
        // restore can race the hide and silently no-op.
        Thread.Sleep(40);

        if (!FocusHelper.ForceForeground(previousWindow))
        {
            return new PasteResult(true, false, "Copied. Could not return focus to the previous window.");
        }

        // Critical for apps like ConnectWise Manage where the outer window
        // regaining foreground does NOT automatically restore the inner edit
        // control's focus. Without this, Ctrl+V has no text box to land in.
        if (previousFocusedControl != IntPtr.Zero
            && previousFocusedControl != previousWindow)
        {
            FocusHelper.RestoreInnerFocus(previousFocusedControl);
        }

        Thread.Sleep(Math.Max(0, settings.PasteDelayMs));

        try
        {
            SendKeys.SendWait("^v");
        }
        catch (Exception ex)
        {
            ScheduleClipboardRestore(previousClipboard, settings.ClipboardRestoreDelayMs);
            return new PasteResult(true, false, "Copied. Paste failed: " + ex.Message);
        }

        ScheduleClipboardRestore(previousClipboard, settings.ClipboardRestoreDelayMs);
        return new PasteResult(true, true, null);
    }

    private static void ScheduleClipboardRestore(string? previous, int delayMs)
    {
        if (previous == null) return;
        var ui = SynchronizationContext.Current;
        Task.Run(async () =>
        {
            await Task.Delay(Math.Max(0, delayMs)).ConfigureAwait(false);
            if (ui != null)
            {
                ui.Post(_ => ClipboardService.RestoreText(previous), null);
            }
            else
            {
                // Clipboard requires STA. If no UI context, just try once.
                try { ClipboardService.RestoreText(previous); } catch { /* ignore */ }
            }
        });
    }
}

public record PasteResult(bool Copied, bool Pasted, string? Message);
