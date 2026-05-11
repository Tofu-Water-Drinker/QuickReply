using System.Runtime.InteropServices;
using QuickReply.Models;

namespace QuickReply;

public class PasteService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private readonly SettingsService _settingsService;

    public PasteService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public static IntPtr CaptureForegroundWindow() => GetForegroundWindow();

    /// <summary>
    /// Puts <paramref name="text"/> on the clipboard. If a previous foreground window
    /// is supplied and AutoPaste is enabled, restores focus and sends Ctrl+V.
    /// </summary>
    public PasteResult PasteOrCopy(string text, IntPtr previousWindow)
    {
        var settings = _settingsService.Current;
        var previousClipboard = settings.RestoreClipboardAfterPaste
            ? ClipboardService.SaveText()
            : null;

        if (!ClipboardService.SetText(text))
        {
            return new PasteResult(false, false, "Clipboard is unavailable.");
        }

        if (!settings.AutoPaste || previousWindow == IntPtr.Zero)
        {
            return new PasteResult(true, false, settings.AutoPaste
                ? "Copied (no target window)."
                : "Copied. AutoPaste is disabled.");
        }

        var focusRestored = SetForegroundWindow(previousWindow);
        if (!focusRestored)
        {
            return new PasteResult(true, false, "Copied. Could not return focus to the previous window.");
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
