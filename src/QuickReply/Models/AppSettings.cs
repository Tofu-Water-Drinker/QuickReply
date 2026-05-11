namespace QuickReply.Models;

public class AppSettings
{
    public bool AutoPaste { get; set; } = true;
    public bool RestoreClipboardAfterPaste { get; set; } = true;
    public int ClipboardRestoreDelayMs { get; set; } = 2500;
    public int PasteDelayMs { get; set; } = 150;
    public string Theme { get; set; } = "dark";
    public string Hotkey { get; set; } = "Ctrl+Alt+;";
    public bool CheckForUpdatesOnStartup { get; set; } = true;
    public bool RandomizeResponses { get; set; } = true;
    public string SignatureCode { get; set; } = "sig";
}
