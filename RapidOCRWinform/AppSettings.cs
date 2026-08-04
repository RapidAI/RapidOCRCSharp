using System.Text.Json;

namespace RapidOCRWinform;

internal sealed class AppSettings
{
    public int Role3Column { get; set; } = -2;

    public int Role4Column { get; set; } = -1;

    public List<string> EmployeeNames { get; set; } = [];

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
        _ = TrySave(out _);
    }

    public bool TrySave(out string? errorMessage)
    {
        try
        {
            string path = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            errorMessage = null;
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private static string GetSettingsPath()
        => Path.Combine(AppContext.BaseDirectory, "config", "settings.json");
}
