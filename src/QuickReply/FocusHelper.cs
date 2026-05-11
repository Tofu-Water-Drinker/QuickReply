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

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct GUITHREADINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public RECT rcCaret;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private const int SW_SHOW    = 5;
    private const int SW_RESTORE = 9;

    public static IntPtr CurrentForegroundWindow() => GetForegroundWindow();

    /// <summary>
    /// Returns the HWND of the inner control that has keyboard focus in the
    /// current foreground window (which may belong to another process), or
    /// <see cref="IntPtr.Zero"/> if we cannot read it. This is what we need
    /// to capture before stealing focus, otherwise apps like ConnectWise
    /// Manage will not return the cursor to the field the user was typing in.
    /// </summary>
    public static IntPtr CaptureFocusedControl()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero) return IntPtr.Zero;

        var threadId = GetWindowThreadProcessId(foreground, out _);
        if (threadId == 0) return IntPtr.Zero;

        var info = new GUITHREADINFO();
        info.cbSize = Marshal.SizeOf(info);
        if (!GetGUIThreadInfo(threadId, ref info)) return IntPtr.Zero;

        // hwndFocus is the inner edit/text control; fall back to hwndActive
        // (the top-level frame) so we always return something useful.
        return info.hwndFocus != IntPtr.Zero
            ? info.hwndFocus
            : info.hwndActive;
    }

    /// <summary>
    /// Re-focuses an inner control captured by <see cref="CaptureFocusedControl"/>.
    /// Uses AttachThreadInput so SetFocus works across process boundaries.
    /// Call after <see cref="ForceForeground"/> has restored the outer window.
    /// </summary>
    public static bool RestoreInnerFocus(IntPtr innerHwnd)
    {
        if (innerHwnd == IntPtr.Zero || !IsWindow(innerHwnd)) return false;

        var targetThreadId  = GetWindowThreadProcessId(innerHwnd, out _);
        var currentThreadId = GetCurrentThreadId();
        if (targetThreadId == 0) return false;

        if (targetThreadId == currentThreadId)
        {
            return SetFocus(innerHwnd) != IntPtr.Zero;
        }

        var attached = AttachThreadInput(currentThreadId, targetThreadId, true);
        try
        {
            SetFocus(innerHwnd);
        }
        finally
        {
            if (attached) AttachThreadInput(currentThreadId, targetThreadId, false);
        }
        return true;
    }

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
