using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TapBpm.Core;

/// <summary>
/// The handful of preferences worth remembering between runs.
/// </summary>
/// <remarks>
/// Stored as JSON under %APPDATA% rather than in the old <c>Settings.settings</c>, which
/// wrote into a version-stamped folder and so silently lost every setting on each update.
/// A corrupt or unreadable file falls back to defaults instead of stopping the app.
/// </remarks>
public sealed class AppSettings
{
    public bool AlwaysOnTop { get; set; }
    public bool GlobalHotkeyEnabled { get; set; } = true;
    public int WindowX { get; set; } = -1;
    public int WindowY { get; set; } = -1;

    [JsonIgnore]
    public bool HasSavedPosition => WindowX >= 0 && WindowY >= 0;

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Vipz", "TapBPM", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // Ignore and start fresh.
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Failing to persist preferences is not worth interrupting the user over.
        }
    }

    public void RememberPosition(Point location)
    {
        WindowX = location.X;
        WindowY = location.Y;
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
