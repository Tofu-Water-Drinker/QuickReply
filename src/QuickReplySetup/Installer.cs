using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace QuickReply.Setup;

internal class Installer
{
    public const string ReleaseDownloadUrl =
        "https://github.com/Tofu-Water-Drinker/QuickReply/releases/latest/download/QuickReply.exe";
    public const string ReleaseDownloadHashUrl =
        "https://github.com/Tofu-Water-Drinker/QuickReply/releases/latest/download/QuickReply.exe.sha256";

    private const string StartupRegistryKey =
        @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "QuickReply";
    private const string PortableFlagFileName = "portable.flag";

    public event Action<int>? ProgressChanged; // 0..100
    public event Action<string>? StatusChanged;

    public async Task InstallAsync(SetupChoices choices, CancellationToken ct = default)
    {
        Status("Preparing install location...");
        Directory.CreateDirectory(choices.InstallPath);

        var exePath = Path.Combine(choices.InstallPath, "QuickReply.exe");
        var dataDir = choices.ResolveDataDirectory();
        Directory.CreateDirectory(dataDir);

        var settingsPath  = Path.Combine(dataDir, "appsettings.json");
        var snippetsPath  = Path.Combine(dataDir, "snippets.json");

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

        Status("Fetching release checksum...");
        var expectedHash = await DownloadHashAsync(ct).ConfigureAwait(false);

        Status("Downloading QuickReply.exe from GitHub...");
        await DownloadExeAsync(exePath, expectedHash, ct).ConfigureAwait(false);

        if (choices.PortableMode)
        {
            // Marker the runtime looks for to keep data next to the EXE.
            Status("Enabling portable mode...");
            File.WriteAllText(Path.Combine(choices.InstallPath, PortableFlagFileName),
                "QuickReply runs in portable mode while this file is present. " +
                "Delete it to move data to %APPDATA%\\QuickReply on next launch.\r\n");
        }
        else
        {
            // Clean up a stale flag from a prior portable install at the same path.
            try
            {
                var stale = Path.Combine(choices.InstallPath, PortableFlagFileName);
                if (File.Exists(stale)) File.Delete(stale);
            }
            catch { /* not fatal */ }
        }

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
            try { if (File.Exists(snippetsPath)) File.Delete(snippetsPath); }
            catch { /* not fatal */ }
        }

        Status("Configuring Windows startup...");
        ConfigureStartup(choices.RunOnStartup, exePath);

        Status("Done.");
        Progress(100);
    }

    private static HttpClient CreateHttpClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("QuickReplySetup/1.0");
        return http;
    }

    /// <summary>
    /// Pulls the published <c>QuickReply.exe.sha256</c> sidecar from the same
    /// GitHub release. Whitespace-tolerant: accepts plain "<hex>" and
    /// shasum-style "<hex>  filename" lines. Returns the hash as a lowercase
    /// hex string. Throws if the file is missing or unparseable, because we
    /// would rather fail the install than skip verification.
    /// </summary>
    private static async Task<string> DownloadHashAsync(CancellationToken ct)
    {
        using var http = CreateHttpClient();
        var raw = await http.GetStringAsync(ReleaseDownloadHashUrl, ct).ConfigureAwait(false);

        // First non-empty line, first whitespace-delimited token.
        string? candidate = null;
        foreach (var line in raw.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;
            candidate = trimmed.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries)[0];
            break;
        }

        if (candidate == null || candidate.Length != 64 || !IsHex(candidate))
        {
            throw new InvalidOperationException(
                "Could not read a valid SHA256 from the release. Expected a 64-character hex " +
                "string in QuickReply.exe.sha256.");
        }
        return candidate.ToLowerInvariant();
    }

    private static bool IsHex(string s)
    {
        foreach (var c in s)
        {
            var isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!isHex) return false;
        }
        return true;
    }

    private async Task DownloadExeAsync(string destPath, string expectedHash, CancellationToken ct)
    {
        using var http = CreateHttpClient();

        using var response = await http.GetAsync(
            ReleaseDownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        await using var input = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

        // Stream to a temp file, hash on the way through, then verify before
        // we move it into place. We never write an unverified binary to the
        // final destination, so a bad hash leaves the previous install (if any)
        // untouched.
        var tempPath = destPath + ".download";
        string actualHash;
        try
        {
            using (var sha = SHA256.Create())
            await using (var output = File.Create(tempPath))
            {
                var buffer = new byte[81920];
                long readTotal = 0;
                int read;
                while ((read = await input.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    readTotal += read;
                    if (totalBytes > 0)
                    {
                        // Download takes up 0..90% of overall progress; the rest is verify + config.
                        var pct = (int)(readTotal * 90 / totalBytes);
                        Progress(Math.Min(90, pct));
                    }
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                actualHash = ToHex(sha.Hash!);
            }

            Status("Verifying checksum...");
            Progress(95);
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Downloaded QuickReply.exe failed checksum verification. " +
                    "The file may be corrupted or tampered with. The bad download has been discarded.\n\n" +
                    $"Expected SHA256: {expectedHash}\n" +
                    $"Got SHA256:      {actualHash}");
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

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
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
            SignatureCode = "sig",
            TutorialShown = false,
            SafeSignatureMode = true
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
