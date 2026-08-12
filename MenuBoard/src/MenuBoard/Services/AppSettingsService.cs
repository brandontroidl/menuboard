using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MenuBoard.Services;

/// <summary>
/// How the app assigns monitors to the two TV displays.
/// </summary>
public enum MonitorLayout
{
    /// <summary>
    /// With 3+ monitors: admin on primary, TVs on the others.
    /// With exactly 2 monitors: both are treated as TVs (typical store PC
    /// that is only hooked up to the two TVs).
    /// </summary>
    Auto,

    /// <summary>Admin keeps the primary monitor; TVs only use non-primary monitors.</summary>
    AdminPlusTvs,

    /// <summary>Every monitor (including primary) is used for TV displays.</summary>
    AllMonitors
}

public class AppSettings
{
    public MonitorLayout MonitorLayout { get; set; } = MonitorLayout.Auto;

    /// <summary>Swap which physical monitor shows TV 1 vs TV 2.</summary>
    public bool SwapDisplays { get; set; }
}

/// <summary>
/// Loads and saves app-level settings (monitor layout, display swap) to a
/// settings.json file next to the executable. Kept outside the database so
/// per-PC hardware configuration travels with the install, not the menu data.
/// </summary>
public class AppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;

    public AppSettings Settings { get; private set; } = new();

    public AppSettingsService(string? directory = null)
    {
        var dir = directory ?? AppDomain.CurrentDomain.BaseDirectory;
        _path = Path.Combine(dir, "settings.json");
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                Settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch
        {
            // Corrupt or unreadable settings file - fall back to defaults
            // rather than crashing a kiosk app at boot.
            Settings = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(Settings, JsonOptions));
        }
        catch
        {
            // Read-only install directory - settings just won't persist.
        }
    }
}
