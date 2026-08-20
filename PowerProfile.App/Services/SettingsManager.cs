using System.Text.Json;
using PowerProfile.App.Models;

namespace PowerProfile.App.Services;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON under
/// %APPDATA%\PowerProfile\settings.json.
/// </summary>
public sealed class SettingsManager
{
    private static readonly string _dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "PowerProfile");

    private static readonly string _path = Path.Combine(_dir, "settings.json");

    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
    };

    public AppSettings Settings { get; private set; } = new();

    // ── Load ─────────────────────────────────────────────────────────────────

    public void Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                Settings = JsonSerializer.Deserialize<AppSettings>(json, _opts) ?? new();
                Settings.NormalizeAnimationPolicies();
            }
        }
        catch
        {
            Settings = new AppSettings();
        }
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    public void Save()
    {
        Directory.CreateDirectory(_dir);
        Settings.NormalizeAnimationPolicies();
        var json = JsonSerializer.Serialize(Settings, _opts);
        File.WriteAllText(_path, json);
    }
}
