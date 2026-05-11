using System.Net.Http;
using System.Text.Json;
using Microsoft.Win32;

namespace QuickReply.Setup;

internal class Installer
{
    public const string ReleaseDownloadUrl =
        "https://github.com/Tofu-Water-Drinker/QuickReply/releases/latest/download/QuickReply.exe";

    private const string StartupRegistryKey =
        @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "QuickReply";

    public event Action<int>? ProgressChanged; // 0..100
    public event Action<string>? StatusChanged;

    public async Task InstallAsync(SetupChoices choices, CancellationToken ct = default)
    {
        Status("Preparing install location...");
        Directory.CreateDirectory(choices.InstallPath);

        var exePath = Path.Combine(choices.InstallPath, "QuickReply.exe");
        var settingsPath = Path.Combine(choices.InstallPath, "appsettings.json");
        var snippetsPath = Path.Combine(choices.InstallPath, "snippets.json");

        // If an old install is running, refuse to overwrite. The exe will be locked.
        if (File.Exists(exePath))
        {
            try
            {
                // Open for write to verify it's not locked. Close immediately.
                using var probe = File.Open(exePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                throw new InvalidOperationException(
                    "QuickReply.exe is already running at the chosen install location. " +
                    "Right-click the tray icon and choose Exit, then re-run setup.");
            }
        }

        Status("Downloading QuickReply.exe from GitHub...");
        await DownloadExeAsync(exePath, ct).ConfigureAwait(false);

        Status("Writing settings...");
        WriteSettings(settingsPath, choices);

        if (choices.Snippets != SnippetMode.Defaults)
        {
            Status("Writing snippets...");
            WriteSnippets(snippetsPath, choices);
        }
        else
        {
            // Make sure no stale snippets.json from a previous install lingers
            // when the user just picked "use the included defaults". The app
            // recreates the default set on first launch when the file is absent.
            // We only delete it if the user actually chose "defaults" and a file
            // was sitting at the chosen location.
            try { if (File.Exists(snippetsPath)) File.Delete(snippetsPath); }
            catch { /* not fatal */ }
        }

        Status("Configuring Windows startup...");
        ConfigureStartup(choices.RunOnStartup, exePath);

        Status("Done.");
        Progress(100);
    }

    private async Task DownloadExeAsync(string destPath, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("QuickReplySetup/1.0");

        using var response = await http.GetAsync(
            ReleaseDownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        await using var input = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

        // Stream to a temp file alongside the destination, then atomically rename.
        var tempPath = destPath + ".download";
        try
        {
            await using (var output = File.Create(tempPath))
            {
                var buffer = new byte[81920];
                long readTotal = 0;
                int read;
                while ((read = await input.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                    readTotal += read;
                    if (totalBytes > 0)
                    {
                        // Download takes up 0..95% of overall progress; the rest is config.
                        var pct = (int)(readTotal * 95 / totalBytes);
                        Progress(Math.Min(95, pct));
                    }
                }
            }

            if (File.Exists(destPath)) File.Delete(destPath);
            File.Move(tempPath, destPath);
        }
        catch
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* ignore */ }
            throw;
        }
    }

    private static void WriteSettings(string path, SetupChoices choices)
    {
        // Mirrors the AppSettings shape used by QuickReply.exe. Keeping defaults
        // in sync here is intentional: a fresh install should produce the same
        // file the app itself would create on first launch.
        var settings = new
        {
            AutoPaste = true,
            RestoreClipboardAfterPaste = true,
            ClipboardRestoreDelayMs = 2500,
            PasteDelayMs = 150,
            Theme = "dark",
            Hotkey = choices.Hotkey,
            CheckForUpdatesOnStartup = true,
            RandomizeResponses = choices.RandomizeResponses,
            SignatureCode = "sig"
        };
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private static void WriteSnippets(string path, SetupChoices choices)
    {
        var dict = choices.Snippets == SnippetMode.Custom
            ? choices.CustomSnippets
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // empty
        var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private static void ConfigureStartup(bool enable, string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: true)
                       ?? Registry.CurrentUser.CreateSubKey(StartupRegistryKey);
        if (key == null) return; // can't happen under normal user, but be safe

        if (enable)
        {
            // Quote the path so spaces (e.g. "Program Files") don't split the command.
            key.SetValue(StartupValueName, $"\"{exePath}\"", RegistryValueKind.String);
        }
        else
        {
            try { key.DeleteValue(StartupValueName, throwOnMissingValue: false); }
            catch { /* not fatal */ }
        }
    }

    private void Progress(int percent) => ProgressChanged?.Invoke(percent);
    private void Status(string text) => StatusChanged?.Invoke(text);
}
