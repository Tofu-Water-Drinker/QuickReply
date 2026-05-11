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

    public static string DefaultInstallPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Programs", "QuickReply");
    }
}

internal enum SnippetMode
{
    Defaults,
    Empty,
    Custom
}
