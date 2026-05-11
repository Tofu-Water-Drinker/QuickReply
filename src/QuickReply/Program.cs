namespace QuickReply;

internal static class Program
{
    private const string MutexName = "Global\\QuickReply.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            // Another instance is already running. Exit quietly.
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        try
        {
            using var context = new TrayApplicationContext();
            Application.Run(context);
        }
        finally
        {
            try { mutex.ReleaseMutex(); } catch { /* ignore */ }
        }
    }
}
