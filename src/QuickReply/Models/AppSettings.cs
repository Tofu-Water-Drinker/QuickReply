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
    public bool TutorialShown { get; set; } = false;

    /// <summary>
    /// When true (default), signature HTML is sanitized before save and before
    /// being placed on the clipboard. Removes &lt;script&gt;, &lt;iframe&gt;,
    /// &lt;object&gt;, &lt;embed&gt;, event-handler attributes (onclick, etc.),
    /// and javascript: URLs. Inline styling, links, images, and basic layout
    /// tags are kept. Turn off only if you know your signature contains
    /// something exotic the sanitizer strips and you have reviewed it yourself.
    /// </summary>
    public bool SafeSignatureMode { get; set; } = true;
}
