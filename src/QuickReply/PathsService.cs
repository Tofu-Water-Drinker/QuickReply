namespace QuickReply;

/// <summary>
/// Single source of truth for where user data lives.
///
/// Default: <c>%APPDATA%\QuickReply\</c>. This is the right place for a per-user
/// Windows app: it survives reinstalls, roams in domains that have profile
/// roaming enabled, and stays clean if the EXE lives in <c>Program Files</c>
/// or another read-only location.
///
/// Portable mode: if a file literally named <c>portable.flag</c> sits next to
/// <c>QuickReply.exe</c>, all data lives next to the EXE instead. This is the
/// USB-stick / drop-in-a-folder workflow, and it is opt-in.
///
/// Migration: on first launch with the new layout, if data files exist next
/// to the EXE but not in <c>%APPDATA%</c>, they are moved (best effort) so an
/// upgrading user keeps their snippets, settings, and signature.
/// </summary>
public static class PathsService
{
    public const string PortableFlagFileName = "portable.flag";

    public const string SnippetsFileName  = "snippets.json";
    public const string SettingsFileName  = "appsettings.json";
    public const string SignatureFileName = "signature.html";

    public static bool IsPortable { get; private set; }
    public static string DataDirectory { get; private set; } = "";
    public static string SnippetsPath  { get; private set; } = "";
    public static string SettingsPath  { get; private set; } = "";
    public static string SignaturePath { get; private set; } = "";

    /// <summary>
    /// Resolves data paths, creates the directory if needed, and one-shot
    /// migrates pre-v1.4 EXE-adjacent files into the new home if it is empty.
    /// Safe to call once on app startup; idempotent thereafter.
    /// </summary>
    public static void Initialize()
    {
        var exeDir = AppContext.BaseDirectory;
        var portableFlag = Path.Combine(exeDir, PortableFlagFileName);
        IsPortable = File.Exists(portableFlag);

        DataDirectory = IsPortable
            ? exeDir
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickReply");

        try { Directory.CreateDirectory(DataDirectory); }
        catch { /* fall through; downstream services will surface the IO error if needed */ }

        SnippetsPath  = Path.Combine(DataDirectory, SnippetsFileName);
        SettingsPath  = Path.Combine(DataDirectory, SettingsFileName);
        SignaturePath = Path.Combine(DataDirectory, SignatureFileName);

        if (!IsPortable)
        {
            MigrateIfNeeded(exeDir, SnippetsFileName,  SnippetsPath);
            MigrateIfNeeded(exeDir, SettingsFileName,  SettingsPath);
            MigrateIfNeeded(exeDir, SignatureFileName, SignaturePath);
        }
    }

    private static void MigrateIfNeeded(string exeDir, string fileName, string destPath)
    {
        try
        {
            var legacyPath = Path.Combine(exeDir, fileName);
            // Only migrate when the legacy file exists and the destination does not.
            // This means upgrades from a pre-v1.4 install carry data forward, but
            // an existing %APPDATA% install never gets clobbered by a stale EXE-side
            // file that the user already moved on past.
            if (!File.Exists(legacyPath) || File.Exists(destPath)) return;
            // Don't try to move a file inside Program Files if QuickReply is installed
            // there read-only; copy is fine, leave the original in place.
            File.Copy(legacyPath, destPath, overwrite: false);
        }
        catch
        {
            // Migration is best-effort. If it fails, the app will create fresh
            // defaults in the new location and the user will lose nothing they
            // can't reproduce.
        }
    }
}
