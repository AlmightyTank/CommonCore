using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public class AssortHelper(
    CommonCoreDb db,
    CoreDebugLogHelper debugLogHelper
)
{
    public void Process(ItemCreationRequest request)
    {
        var assort = request.AdditionalAssortData;
        if (assort == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.TraderId))
        {
            debugLogHelper.LogError("AssortHelper", $"TraderId missing for {request.NewId}");
            return;
        }

        if (!db.Traders.TryGetValue(request.TraderId, out var trader))
        {
            debugLogHelper.LogError("AssortHelper", $"Trader '{request.TraderId}' not found for {request.NewId}");
            return;
        }

        if (trader.Assort == null)
        {
            debugLogHelper.LogError("AssortHelper", $"Trader assort is null for trader '{request.TraderId}'");
            return;
        }

        if (assort.Items == null || assort.BarterScheme == null || assort.LoyalLevelItems == null)
        {
            debugLogHelper.LogError("AssortHelper", $"AdditionalAssortData invalid for {request.NewId}");
            return;
        }

        try
        {
            foreach (var item in assort.Items)
            {
                if (item == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    debugLogHelper.LogError("AssortHelper", $"Skipping assort item with missing _id for {request.NewId}");
                    continue;
                }

                var existingItem = trader.Assort.Items.FirstOrDefault(x => x.Id == item.Id);
                if (existingItem == null)
                {
                    trader.Assort.Items.Add(item);
                }
                else
                {
                    debugLogHelper.LogError("AssortHelper", $"Trader assort item '{item.Id}' already exists for trader '{request.TraderId}', skipping item add.");
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

            debugLogHelper.LogService("AssortHelper", $"Added assort data for {request.NewId} to trader '{request.TraderId}'");
        }
        catch (Exception ex)
        {
            debugLogHelper.LogError("AssortHelper", $"Failed for {request.NewId}: {ex.Message}");
        }
    }
}