using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace CommonCore.Core;

public sealed class CommonCoreSettings
{
    private static readonly MongoId FallbackTraderId = new("5a7c2eca46aef81a7ca2145d");

    public DebugSettings Debug { get; set; } = new();
    public ItemSettings Items { get; set; } = new();
    public TraderSettings Traders { get; set; } = new();
    public QuestSettings Quests { get; set; } = new();

    /// <summary>
    /// Runtime default trader used when a trader mod takes ownership of item routing.
    /// Not intended to be loaded from config.json.
    /// </summary>
    public MongoId DefaultTraderId { get; private set; } = FallbackTraderId;

    /// <summary>
    /// True when a trader mod explicitly takes over all item routing.
    /// Not intended to be loaded from config.json.
    /// </summary>
    public bool ForceAllItemsToDefaultTrader { get; private set; }

    public void SetDefaultTrader(MongoId traderId, bool forceAllItems = true)
    {
        if (string.IsNullOrWhiteSpace(traderId))
        {
            return;
        }

        DefaultTraderId = traderId;
        ForceAllItemsToDefaultTrader = forceAllItems;
    }

    public void ResetDefaultTrader()
    {
        DefaultTraderId = FallbackTraderId;
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