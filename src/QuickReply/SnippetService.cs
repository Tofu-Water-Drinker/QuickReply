using System.Text.Json;
using System.Text.RegularExpressions;

namespace QuickReply;

public class SnippetService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex DateTokenRegex = new(
        @"\{\{date:([^}]+)\}\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private Dictionary<string, string> _snippets = new(StringComparer.OrdinalIgnoreCase);

    public string FilePath { get; }
    public IReadOnlyDictionary<string, string> Snippets => _snippets;
    public event EventHandler? Reloaded;

    public SnippetService(string filePath)
    {
        FilePath = filePath;
    }

    public bool LoadOrCreate()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                WriteDefaults();
            }

            var json = File.ReadAllText(FilePath);
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
            if (parsed == null)
            {
                throw new InvalidDataException("snippets.json is empty or invalid.");
            }

            _snippets = new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
            Reloaded?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to load snippets.json.\n\n{ex.Message}\n\n" +
                (_snippets.Count > 0
                    ? "Keeping previously loaded snippets."
                    : "No snippets are currently loaded."),
                "QuickReply",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
    }

    public bool Contains(string code) =>
        !string.IsNullOrWhiteSpace(code) && _snippets.ContainsKey(code.Trim());

    /// <summary>
    /// Adds a new snippet or replaces an existing one (case-insensitive on code),
    /// then writes snippets.json. Raises <see cref="Reloaded"/> on success.
    /// </summary>
    public bool AddOrUpdate(string code, string text)
    {
        code = code?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(code)) return false;

        try
        {
            var dict = new Dictionary<string, string>(_snippets, StringComparer.OrdinalIgnoreCase);
            dict[code] = text ?? string.Empty;

            var json = JsonSerializer.Serialize(dict, JsonOptions);
            File.WriteAllText(FilePath, json);

            _snippets = dict;
            Reloaded?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not save snippet to snippets.json.\n\n{ex.Message}",
                "QuickReply",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
    }

    public bool TryGet(string code, out string text)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            text = string.Empty;
            return false;
        }

        if (_snippets.TryGetValue(code.Trim(), out var raw))
        {
            text = ExpandTokens(raw);
            return true;
        }

        text = string.Empty;
        return false;
    }

    public string ExpandTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        return DateTokenRegex.Replace(text, m =>
        {
            var format = m.Groups[1].Value.Trim();
            try
            {
                return DateTime.Now.ToString(format);
            }
            catch
            {
                return m.Value;
            }
        });
    }

    private void WriteDefaults()
    {
        File.WriteAllText(FilePath, DefaultSnippetsJson);
    }

    private const string DefaultSnippetsJson = """
{
  "fu": "Following up on this ticket. Are you still experiencing the issue, or are we okay to close this out?",
  "vm": "I left you a voicemail and will follow up again if I do not hear back.",
  "close": "I am going to mark this ticket resolved for now. If the issue returns, reply here and we can reopen or continue troubleshooting.",
  "rbt": "Please reboot the computer when you have a chance, then let me know if the issue continues afterward.",
  "ty": "Thank you! I appreciate the update.",

  "dig": "I’m going to dig into this and will update you once I have more useful information.",
  "next": "The next step is for me to check a few things on our side. I’ll follow up here with what I find.",
  "vendor": "This appears to be tied to a vendor-side issue. I’m checking what options we have and will keep you updated.",
  "wait": "I do not have a clean answer yet, and I do not want to guess. I’m going to verify this first and then update you.",

  "date": "{{date:yyyy-MM-dd}}",
  "time": "{{date:yyyy-MM-dd h:mm tt}}",

  "ts": "Troubleshooting performed:\n- \n\nResult:\n- \n\nNext step:\n- ",
  "note": "Issue:\n- \n\nTroubleshooting:\n- \n\nResolution / next step:\n- ",
  "cb": "Callback requested.\nBest number:\nBest time:\nNotes:",
  "esc": "Escalation summary:\n\nIssue:\n- \n\nImpact:\n- \n\nTroubleshooting completed:\n- \n\nWhat we need next:\n- ",

  "heads": "Heads up: we are currently looking into this. I’ll update this thread once we have a clearer picture of impact and next steps.",
  "impact": "Current impact:\n- \n\nKnown scope:\n- \n\nCurrent action:\n- \n\nNext update:",
  "resolved": "This appears to be resolved now. We’ll keep an eye on it, but no further action should be needed unless the issue returns.",
  "monitoring": "The immediate issue appears stable, but we are continuing to monitor before calling it fully resolved.",
  "rootcause": "Known cause:\n- \n\nWhat happened:\n- \n\nWhat fixed it:\n- \n\nPrevention / follow-up:\n- ",

  "caliente": "Caliente - Moderate impact. Localized issue or roughly 25% of users affected.",
  "picante": "Picante - Moderate-high impact. Major system issue or roughly 50% affected or major system outage.",
  "fuego": "Fuego - High impact. Multiple systems/clients affected or roughly 75% of users affected.",
  "inferno": "Inferno - Critical impact. Entire company/client environment down or all users affected.",
  "scorched": "Scorched - Escalation risk. VIP/client angry, SLA risk, or leadership visibility needed.",
  "hellfire": "Hellfire - High impact. Multi-client or widespread impact.",
  "meltdown": "Meltdown - Critical security impact. Security breach, ransomware, data loss, or active compromise concern.",
  "zing": "Zing - Moderate impact. Vendor-caused or external dependency issue.",

  "rebootsteps": "Please try the following when you have a chance:\n\n1. Save any open work.\n2. Restart the computer.\n3. Sign back in and test again.\n4. Reply here with whether the issue is still happening.",
  "screenshot": "Can you send a screenshot of the error or issue when you get a chance? That will help us confirm exactly what you are seeing.",
  "testagain": "Can you test it again now and let me know if the behavior changed?",
  "confirmuser": "Can you confirm the username or email address affected by this issue?",

  "vendorcase": "We have opened a case with the vendor and are waiting on their response. I’ll update this ticket once we have more information from them.",
  "waitinguser": "Waiting on user response. We will continue once we hear back.",
  "waitingvendor": "Waiting on vendor response. We will update once the vendor provides next steps.",
  "waitingapproval": "Waiting on approval before proceeding."
}
""";
}
