using System.Text.Json;
using System.IO;

namespace Reverse1999UrlCatcher.App.Services;

public sealed record AppSettings(
    string? AdbPath = null,
    string? MitmdumpPath = null,
    string? LastHostIp = null,
    string? LastSerial = null,
    int ProxyPort = 8877,
    bool IsDarkTheme = true);

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _path;

    public AppSettingsStore(string? path = null)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Reverse1999UrlCatcher");
        Directory.CreateDirectory(directory);
        _path = path ?? Path.Combine(directory, "appsettings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_path))
        {
            var defaultPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(defaultPath))
            {
                try
                {
                    var defaultText = File.ReadAllText(defaultPath);
                    return JsonSerializer.Deserialize<AppSettings>(defaultText, Options) ?? new AppSettings();
                }
                catch
                {
                    return new AppSettings();
                }
            }

            return new AppSettings();
        }

        try
        {
            var text = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<AppSettings>(text, Options) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var text = JsonSerializer.Serialize(settings, Options);
        File.WriteAllText(_path, text);
    }
}
