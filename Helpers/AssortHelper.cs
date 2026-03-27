using CommonCore.Constants;
using CommonCore.Core;
using CommonCore.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonCore.Helpers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public sealed class AssortHelper(
    DatabaseService databaseService,
    CoreDebugLogHelper debugLogHelper,
    CommonCoreSettings settings)
{
    public void Process(CommonCoreItemRequest request)
    {
        var assort = request.Config.AdditionalAssortData;
        if (assort == null)
        {
            return;
        }

        if (request.Config.Traders == null || request.Config.Traders.Count == 0)
        {
            debugLogHelper.LogError("AssortHelper", $"No traders configured for {request.ItemId}");
            return;
        }

        if (assort.Items == null || assort.BarterScheme == null || assort.LoyalLevelItems == null)
        {
            debugLogHelper.LogError("AssortHelper", $"AdditionalAssortData invalid for {request.ItemId}");
            return;
        }

        foreach (var (traderKey, assortEntries) in request.Config.Traders)
        {
            if (assortEntries == null || assortEntries.Count == 0)
            {
                debugLogHelper.LogService("AssortHelper", $"No assort entries for trader '{traderKey}' on {request.ItemId}");
                continue;
            }

            var traderId = ResolveTraderId(traderKey);
            if (string.IsNullOrWhiteSpace(traderId))
            {
                debugLogHelper.LogError("AssortHelper", $"Could not resolve trader '{traderKey}' for {request.ItemId}");
                continue;
            }

            if (!databaseService.GetTraders().TryGetValue(traderId, out var trader))
            {
                debugLogHelper.LogError("AssortHelper", $"Trader '{traderId}' not found for {request.ItemId}");
                continue;
            }

            if (trader.Assort == null)
            {
                debugLogHelper.LogError("AssortHelper", $"Trader assort is null for trader '{traderId}'");
                continue;
            }

            try
            {
                foreach (var (assortId, config) in assortEntries)
                {
                    if (config == null)
                    {
                        continue;
                    }

                    debugLogHelper.LogService(
                        "AssortHelper",
                        $"Processing assortId={assortId} for traderKey='{traderKey}' traderId='{traderId}' item={request.ItemId}");

                    foreach (var item in assort.Items)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(item.Id))
                        {
                            debugLogHelper.LogError("AssortHelper", $"Skipping assort item with missing _id for {request.ItemId}");
                            continue;
                        }

                        var existingItem = trader.Assort.Items.FirstOrDefault(x => x.Id == item.Id);
                        if (existingItem == null)
                        {
                            trader.Assort.Items.Add(item);
                        }
                        else
                        {
                            debugLogHelper.LogError(
                                "AssortHelper",
                                $"Trader assort item '{item.Id}' already exists for trader '{traderId}', skipping item add.");
                        }
                    }

                    foreach (var kvp in assort.BarterScheme)
                    {
                        trader.Assort.BarterScheme[kvp.Key] = kvp.Value;
                    }

                    foreach (var kvp in assort.LoyalLevelItems)
                    {
                        trader.Assort.LoyalLevelItems[kvp.Key] = kvp.Value;
                    }

                    debugLogHelper.LogService(
                        "AssortHelper",
                        $"Added assort data for {request.ItemId} to trader '{traderId}' using assort '{assortId}'");
                }
            }
            catch (Exception ex)
            {
                debugLogHelper.LogError("AssortHelper", $"Failed for {request.ItemId}: {ex}");
            }
        }
    }

    private string? ResolveTraderId(string? traderKey)
    {
        if (settings.ForceAllItemsToDefaultTrader)
        {
            return settings.DefaultTraderId;
        }

        if (string.IsNullOrWhiteSpace(traderKey))
        {
            return settings.DefaultTraderId;
        }

        if (ItemMaps.TraderMap.TryGetValue(traderKey.ToLowerInvariant(), out var mappedTraderId))
        {
            return mappedTraderId;
        }

        return traderKey;
    }
}