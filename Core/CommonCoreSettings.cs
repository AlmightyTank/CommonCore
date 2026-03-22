using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace CommonCore.Core;

[Injectable(InjectionType.Singleton)]
public sealed class CommonCoreSettings
{
    private static readonly MongoId FallbackTraderId = new("5a7c2eca46aef81a7ca2145d");

    public MongoId DefaultTraderId { get; private set; } = FallbackTraderId;

    // true when a trader mod explicitly takes over all item routing
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