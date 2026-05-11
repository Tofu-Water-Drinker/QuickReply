using System.Reflection;
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

    private const string DefaultsResourceName = "QuickReply.snippets-defaults.json";
    private const int MaxAliasDepth = 8;

    private Dictionary<string, SnippetEntry> _snippets =
        new(StringComparer.OrdinalIgnoreCase);

    public string FilePath { get; }
    public IReadOnlyDictionary<string, SnippetEntry> Snippets => _snippets;

    /// <summary>
    /// Entries from <c>snippets.json</c> the parser had to skip on the last
    /// load (wrong JSON type, empty array, etc.). Useful so the UI can show
    /// the user exactly what was ignored instead of silently dropping it.
    /// </summary>
    public IReadOnlyList<SnippetLoadIssue> LastLoadIssues { get; private set; } = Array.Empty<SnippetLoadIssue>();

    public event EventHandler? Reloaded;

    public SnippetService(string filePath)
    {
        FilePath = filePath;
    }

    // ── Loading / saving ─────────────────────────────────────────────────

    public bool LoadOrCreate()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                WriteDefaults();
            }

            var json = File.ReadAllText(FilePath);
            var (parsed, issues) = ParseSnippetsJson(json);
            _snippets = parsed;
            LastLoadIssues = issues;
            Reloaded?.Invoke(this, EventArgs.Empty);

            if (issues.Count > 0)
            {
                var preview = string.Join("\n",
                    issues.Take(8).Select(i => $"  - \"{i.Code}\": {i.Reason}"));
                if (issues.Count > 8) preview += $"\n  ... and {issues.Count - 8} more.";
                MessageBox.Show(
                    $"Loaded {_snippets.Count} snippets, but skipped {issues.Count} invalid " +
                    $"entr{(issues.Count == 1 ? "y" : "ies")} in snippets.json:\n\n{preview}\n\n" +
                    "Fix or remove these entries and click Reload Snippets.",
                    "QuickReply",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
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

    /// <summary>
    /// Looks up <paramref name="code"/>, resolves aliases, picks a variant,
    /// and expands date tokens. Picks a random variant when
    /// <paramref name="randomize"/> is true, otherwise returns the first.
    /// </summary>
    public bool TryResolve(string code, bool randomize, out string text)
    {
        text = string.Empty;
        if (string.IsNullOrWhiteSpace(code)) return false;
        return ResolveCode(code.Trim(), randomize, depth: 0, out text);
    }

    /// <summary>Backward-compat shim. Always uses randomization.</summary>
    public bool TryGet(string code, out string text) => TryResolve(code, randomize: true, out text);

    public bool Contains(string code) =>
        !string.IsNullOrWhiteSpace(code) && _snippets.ContainsKey(code.Trim());

    private bool ResolveCode(string code, bool randomize, int depth, out string text)
    {
        text = string.Empty;
        if (depth > MaxAliasDepth) return false; // alias loop / chain too deep
        if (!_snippets.TryGetValue(code, out var entry)) return false;

        if (entry.IsAlias)
        {
            return ResolveCode(entry.AliasTarget!, randomize, depth + 1, out text);
        }

        if (entry.Variants.Length == 0) return false;
        var raw = entry.Variants.Length == 1
            ? entry.Variants[0]
            : entry.Variants[randomize ? Random.Shared.Next(entry.Variants.Length) : 0];
        text = ExpandTokens(raw);
        return true;
    }

    public string ExpandTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return DateTokenRegex.Replace(text, m =>
        {
            var format = m.Groups[1].Value.Trim();
            try { return DateTime.Now.ToString(format); }
            catch { return m.Value; }
        });
    }

    // ── Editing ──────────────────────────────────────────────────────────

    public bool AddOrUpdate(string code, string text) =>
        AddOrUpdate(code, new[] { text });

    public bool AddOrUpdate(string code, IEnumerable<string> variants)
    {
        code = code?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(code)) return false;

        var list = variants?
            .Select(v => v ?? string.Empty)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray()
            ?? Array.Empty<string>();
        if (list.Length == 0) return false;

        return WriteWithChange(d => d[code] = SnippetEntry.WithVariants(list));
    }

    public bool AddAlias(string code, string target)
    {
        code = code?.Trim() ?? string.Empty;
        target = target?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(target)) return false;
        return WriteWithChange(d => d[code] = SnippetEntry.WithAlias(target));
    }

    public bool Remove(string code)
    {
        code = code?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(code)) return false;
        return WriteWithChange(d => d.Remove(code));
    }

    private bool WriteWithChange(Action<Dictionary<string, SnippetEntry>> mutate)
    {
        try
        {
            var dict = new Dictionary<string, SnippetEntry>(_snippets, StringComparer.OrdinalIgnoreCase);
            mutate(dict);
            var json = SerialiseSnippets(dict);
            File.WriteAllText(FilePath, json);
            _snippets = dict;
            Reloaded?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not save snippets.json.\n\n{ex.Message}",
                "QuickReply",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
    }

    // ── JSON helpers ─────────────────────────────────────────────────────

    private static (Dictionary<string, SnippetEntry> Snippets, List<SnippetLoadIssue> Issues) ParseSnippetsJson(string json)
    {
        var result = new Dictionary<string, SnippetEntry>(StringComparer.OrdinalIgnoreCase);
        var issues = new List<SnippetLoadIssue>();
        if (string.IsNullOrWhiteSpace(json)) return (result, issues);

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object) return (result, issues);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var code = prop.Name;
            if (string.IsNullOrWhiteSpace(code))
            {
                issues.Add(new SnippetLoadIssue("(blank)", "Snippet code cannot be empty."));
                continue;
            }

            switch (prop.Value.ValueKind)
            {
                case JsonValueKind.String:
                    var s = prop.Value.GetString() ?? string.Empty;
                    if (s.StartsWith('@'))
                    {
                        var target = s[1..].Trim();
                        if (string.IsNullOrEmpty(target))
                        {
                            issues.Add(new SnippetLoadIssue(code, "Alias has no target (just '@')."));
                            break;
                        }
                        result[code] = SnippetEntry.WithAlias(target);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(s))
                        {
                            issues.Add(new SnippetLoadIssue(code, "Reply text is empty."));
                            break;
                        }
                        result[code] = SnippetEntry.WithVariants(new[] { s });
                    }
                    break;

                case JsonValueKind.Array:
                    var list = new List<string>();
                    var arrayIndex = 0;
                    foreach (var item in prop.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var v = item.GetString() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(v))
                            {
                                issues.Add(new SnippetLoadIssue(code,
                                    $"Variant at index {arrayIndex} is empty; skipped."));
                            }
                            else
                            {
                                list.Add(v);
                            }
                        }
                        else
                        {
                            issues.Add(new SnippetLoadIssue(code,
                                $"Variant at index {arrayIndex} is {DescribeKind(item.ValueKind)}; only strings are allowed."));
                        }
                        arrayIndex++;
                    }
                    if (list.Count > 0)
                    {
                        result[code] = SnippetEntry.WithVariants(list.ToArray());
                    }
                    else
                    {
                        issues.Add(new SnippetLoadIssue(code, "No usable string variants in array."));
                    }
                    break;

                case JsonValueKind.Null:
                    issues.Add(new SnippetLoadIssue(code, "Value is null."));
                    break;

                default:
                    issues.Add(new SnippetLoadIssue(code,
                        $"Value is {DescribeKind(prop.Value.ValueKind)}; expected a string, array of strings, or \"@target\" alias."));
                    break;
            }
        }
        return (result, issues);
    }

    private static string DescribeKind(JsonValueKind kind) => kind switch
    {
        JsonValueKind.True or JsonValueKind.False => "a boolean",
        JsonValueKind.Number => "a number",
        JsonValueKind.Object => "an object",
        JsonValueKind.Array  => "an array",
        JsonValueKind.Null   => "null",
        _ => kind.ToString().ToLowerInvariant()
    };

    private static string SerialiseSnippets(Dictionary<string, SnippetEntry> snippets)
    {
        // Write strings as plain values, arrays as arrays, aliases as "@target".
        // Using Dictionary<string, object?> so System.Text.Json picks the right
        // JSON kind per entry.
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, entry) in snippets)
        {
            if (entry.IsAlias)            payload[code] = "@" + entry.AliasTarget;
            else if (entry.Variants.Length == 1) payload[code] = entry.Variants[0];
            else                          payload[code] = entry.Variants;
        }
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private void WriteDefaults()
    {
        File.WriteAllText(FilePath, ReadDefaultsResource());
    }

    private static string ReadDefaultsResource()
    {
        var asm = typeof(SnippetService).Assembly;
        using var stream = asm.GetManifestResourceStream(DefaultsResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{DefaultsResourceName}' is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

public sealed record SnippetLoadIssue(string Code, string Reason);

public class SnippetEntry
{
    public bool IsAlias { get; init; }
    public string? AliasTarget { get; init; }
    public string[] Variants { get; init; } = Array.Empty<string>();

    public int VariantCount => IsAlias ? 0 : Variants.Length;

    public static SnippetEntry WithVariants(string[] variants) =>
        new() { Variants = variants };

    public static SnippetEntry WithAlias(string target) =>
        new() { IsAlias = true, AliasTarget = target };
}
