namespace CommonCore.Traders.Services;

using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Traders.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Servers;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Path = System.IO.Path;

[Injectable]
public sealed class TraderConfigService(CoreDebugLogHelper debugLogHelper)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public TraderRuntimeSettings LoadOrCreate(
        string modPath,
        ITraderDefinition definition)
    {
        var fullConfigPath = Path.Combine(modPath, definition.ConfigFilePath);

        if (!File.Exists(fullConfigPath))
        {
            debugLogHelper.LogError("TraderConfigService", $"[TraderConfigService] Config file not found at '{fullConfigPath}'. Using defaults.");
            return new TraderRuntimeSettings();
        }

        try
        {
            var json = File.ReadAllText(fullConfigPath);

            var runtimeSettings = JsonSerializer.Deserialize<TraderRuntimeSettings>(json, JsonOptions);
            if (runtimeSettings != null && HasMeaningfulValues(runtimeSettings))
            {
                debugLogHelper.LogService("TraderConfigService", $"Loaded TraderRuntimeSettings from '{fullConfigPath}'.");
                return runtimeSettings;
            }
        }
        catch (Exception ex)
        {
            debugLogHelper.LogError("TraderConfigService", $"[TraderConfigService] Failed to load config '{fullConfigPath}': {ex.Message}");
        }

        debugLogHelper.LogError("TraderConfigService", $"[TraderConfigService] Falling back to default TraderRuntimeSettings for '{definition.TraderId}'.");
        return new TraderRuntimeSettings();
    }

    private static bool HasMeaningfulValues(TraderRuntimeSettings settings)
    {
        return settings is not null;
    }
}