namespace QuickReply.Setup;

internal class SetupChoices
{
    public string InstallPath { get; set; } = DefaultInstallPath();
    public string Hotkey { get; set; } = "Ctrl+Alt+;";
    public SnippetMode Snippets { get; set; } = SnippetMode.Defaults;

    /// <summary>
    /// Only populated when <see cref="Snippets"/> is <see cref="SnippetMode.Custom"/>.
    /// Maps snippet code to reply text.
    /// </summary>
    public Dictionary<string, string> CustomSnippets { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool RunOnStartup { get; set; } = true;
    public bool LaunchAfterInstall { get; set; } = true;
    public bool RandomizeResponses { get; set; } = true;

    /// <summary>
    /// If true, QuickReply keeps all data (snippets, settings, signature) next
    /// to <c>QuickReply.exe</c> instead of in <c>%APPDATA%\QuickReply</c>.
    /// The installer drops a <c>portable.flag</c> file next to the exe to flip
    /// the runtime into portable mode.
    /// </summary>
    public bool PortableMode { get; set; } = false;

    public static string DefaultInstallPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Programs", "QuickReply");
    }

    /// <summary>
    /// Where snippets / settings / signature should be written for this install.
    /// Mirrors <c>PathsService</c> in the main app: <c>%APPDATA%\QuickReply</c>
    /// by default, or next to the EXE when portable mode is on.
    /// </summary>
    public string ResolveDataDirectory() =>
        PortableMode
            ? InstallPath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickReply");
}

internal enum SnippetMode
{
    Defaults,
    Empty,
    Custom
}
