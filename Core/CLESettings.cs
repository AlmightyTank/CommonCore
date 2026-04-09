using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using System.Text.Json.Serialization;

namespace CommonLibExtended.Core;

[Injectable]
public class CLESettings
{
    private static readonly MongoId FallbackTraderId = new("5a7c2eca46aef81a7ca2145d");

    public DebugSettings Debug { get; set; } = new();
    public ItemSettings Items { get; set; } = new();
    public TraderSettings Traders { get; set; } = new();
    public QuestSettings Quests { get; set; } = new();
    [JsonPropertyName("forceAllItemsToDefaultTrader")]
    public bool ForceAllItemsToDefaultTrader { get; set; } = false;

    [JsonPropertyName("defaultTraderId")]
    public string DefaultTraderId { get; set; } = "5a7c2eca46aef81a7ca2145d";

    public void SetDefaultTrader(MongoId traderId, bool forceAllItems = true)
    {
        if (string.IsNullOrWhiteSpace(traderId))
        {
            return;
        }

        DefaultTraderId = traderId.ToString();
        ForceAllItemsToDefaultTrader = forceAllItems;
    }

    public void ResetDefaultTrader()
    {
        DefaultTraderId = FallbackTraderId.ToString();
        ForceAllItemsToDefaultTrader = false;
    }
}

public sealed class DebugSettings
{
    /// <summary>
    /// Master permission switch. If false, no debug logs are written.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// If true, logs everything as long as Enabled is true.
    /// </summary>
    public bool ForceAll { get; set; } = false;

    /// <summary>
    /// Per-file/class switches. Example: "QuestAssortHelper": true
    /// </summary>
    public Dictionary<string, bool> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Per-function switches. Example: "QuestAssortHelper.Process": true
    /// </summary>
    public Dictionary<string, bool> Functions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ItemSettings
{
    public bool Enabled { get; set; } = true;
    public bool ContinueOnFileError { get; set; } = true;
    public bool LogMissingFolders { get; set; } = true;
    public bool LogProcessedFiles { get; set; } = false;
}

public sealed class TraderSettings
{
    public bool Enabled { get; set; } = true;
    public bool EnableTraderAssorts { get; set; } = true;
    public bool EnablePresetTraderOffers { get; set; } = true;
    public bool AllowDefaultTraderFallback { get; set; } = true;
}

public sealed class QuestSettings
{
    public bool Enabled { get; set; } = true;
    public bool EnableQuestAssorts { get; set; } = true;
    public bool EnableQuestRewards { get; set; } = true;
}