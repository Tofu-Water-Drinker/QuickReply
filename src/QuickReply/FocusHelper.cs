using System.Runtime.InteropServices;

namespace QuickReply;

/// <summary>
/// Win32 plumbing for reliably returning focus to a window that QuickReply
/// previously stole away. Plain SetForegroundWindow is unreliable on modern
/// Windows because of focus-stealing prevention; the AttachThreadInput trick
/// temporarily ties our input queue to the foreground and target threads so
/// the OS treats us as if we have permission to change foreground.
/// </summary>
internal static class FocusHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(
        uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    private const int SW_SHOW    = 5;
    private const int SW_RESTORE = 9;

    public static IntPtr CurrentForegroundWindow() => GetForegroundWindow();

    /// <summary>
    /// Brings <paramref name="hWnd"/> to the foreground, restoring it if it is
    /// minimised. Returns true only if the target actually becomes foreground.
    /// </summary>
    public static bool ForceForeground(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero || !IsWindow(hWnd)) return false;

        // Restore minimised windows first so SetForegroundWindow has a window
        // it can actually display.
        if (IsIconic(hWnd))
        {
            ShowWindow(hWnd, SW_RESTORE);
        }

        // Fast path: already foreground.
        var foreground = GetForegroundWindow();
        if (foreground == hWnd) return true;

        var currentThreadId    = GetCurrentThreadId();
        var foregroundThreadId = foreground != IntPtr.Zero
            ? GetWindowThreadProcessId(foreground, out _)
            : 0u;
        var targetThreadId     = GetWindowThreadProcessId(hWnd, out _);

        var attachedForeground = false;
        var attachedTarget     = false;

        if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId)
        {
            attachedForeground = AttachThreadInput(currentThreadId, foregroundThreadId, true);
        }
        if (targetThreadId != 0
            && targetThreadId != currentThreadId
            && targetThreadId != foregroundThreadId)
        {
            attachedTarget = AttachThreadInput(currentThreadId, targetThreadId, true);
        }

        try
        {
            BringWindowToTop(hWnd);
            ShowWindow(hWnd, SW_SHOW);
            SetForegroundWindow(hWnd);
        }
        finally
        {
            if (attachedForeground) AttachThreadInput(currentThreadId, foregroundThreadId, false);
            if (attachedTarget)     AttachThreadInput(currentThreadId, targetThreadId,     false);
        }

        return GetForegroundWindow() == hWnd;
    }
}
