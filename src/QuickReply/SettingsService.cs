using System.Text.Json;
using QuickReply.Models;

namespace QuickReply;

public class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string FilePath { get; }
    public AppSettings Current { get; private set; } = new();

    public SettingsService(string filePath)
    {
        FilePath = filePath;
    }

    public AppSettings LoadOrCreate()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                Current = new AppSettings();
                Save(Current);
                return Current;
            }

            var json = File.ReadAllText(FilePath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            Current = loaded ?? new AppSettings();
            return Current;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to read appsettings.json. Using defaults.\n\n{ex.Message}",
                "QuickReply",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            Current = new AppSettings();
            return Current;
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Best-effort. Don't crash the app over a settings save failure.
        }
    }
}
