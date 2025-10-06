using System.Text.Json;

//This class is for getting config and saving changes

public class BotConfig
{
    public string BotId { get; set; } = string.Empty;
    public string AdminPass { get; set; } = string.Empty;
    public string AdminId { get; set; } = string.Empty;
    public string DayStart { get; set; } = string.Empty;
    public string DayEnd { get; set; } = string.Empty;

    public static BotConfig Load()
    {
        string json = File.ReadAllText("config.json");
        return JsonSerializer.Deserialize<BotConfig>(json) ?? new BotConfig();
    }

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText("config.json", json);
    }
}