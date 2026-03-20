using SPTarkov.Server.Core.Models.Common;
using CommonCore.Constants;

namespace CommonCore.Helpers;

public static class TraderIdsHelper
{
    public static void Add(string traderName, MongoId traderId)
    {
        if (ItemMaps.TraderMap.TryGetValue(traderName, out _)) return;
        ItemMaps.TraderMap[traderName] = traderId;
    }

    public static void Update(string traderName, MongoId traderId)
    {
        if (ItemMaps.TraderMap.TryGetValue(traderName, out var trader))
        {
            if (trader == traderId) return;
            ItemMaps.TraderMap[traderName] = traderId;
        }
        else
        {
            Add(traderName, traderId);
        }
    }
}