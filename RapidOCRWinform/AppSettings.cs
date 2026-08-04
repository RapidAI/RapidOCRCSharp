using System.Text.Json;

namespace RapidOCRWinform;

internal sealed class AppSettings
{
    public int Role3Column { get; set; } = -2;

    public int Role4Column { get; set; } = -1;

    public static AppSettings Load()
    {
        try
        {
            string path = GetSettingsPath();
            return File.Exists(path)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings()
                : new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            string path = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Settings are a convenience only; inability to persist them must not block OCR.
        }
    }

    private static string GetSettingsPath()
    {
        string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localData, "RapidOCRSchedule", "settings.json");
    }
}
