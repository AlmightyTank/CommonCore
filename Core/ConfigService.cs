using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Services;
using System.Reflection;
using System.Text.Json;

namespace CommonCore.Core;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public sealed class ConfigService(CommonCoreSettings settings) : IOnLoad
{
    private readonly CommonCoreSettings _settings = settings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public async Task OnLoad()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var modPath = Path.GetDirectoryName(assembly.Location);

        if (string.IsNullOrWhiteSpace(modPath))
        {
            Console.WriteLine("[CommonCore] Could not resolve mod path, using default settings.");
            return;
        }

        var configPath = Path.Combine(modPath, "config.json");

        if (!File.Exists(configPath))
        {
            Console.WriteLine($"[CommonCore] No config.json found at {configPath}, using defaults.");
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            var loaded = JsonSerializer.Deserialize<CommonCoreSettings>(json, JsonOptions);

            if (loaded == null)
            {
                Console.WriteLine("[CommonCore] config.json could not be parsed, using defaults.");
                return;
            }

            _settings.Debug = loaded.Debug ?? new();
            _settings.Items = loaded.Items ?? new();
            _settings.Traders = loaded.Traders ?? new();
            _settings.Quests = loaded.Quests ?? new();

            Console.WriteLine("[CommonCore] Config loaded.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CommonCore] Failed to load config.json: {ex}");
        }
    }
}