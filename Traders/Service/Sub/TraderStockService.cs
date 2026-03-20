using CommonCore.Helpers;
using CommonCore.Traders.Models;
using SPTarkov.DI.Annotations;

namespace CommonCore.Traders.Service.Sub;

[Injectable]
public sealed class TraderStockService(
    CoreDebugLogHelper debugLogService)
{
    public void Apply(TraderLoadContext context)
    {
        var settings = context.Settings;

        if (!settings.RandomizeStockAvailable && !settings.UnlimitedStock)
        {
            debugLogService.LogService("Stock", "Skipped stock processing. No stock modifiers enabled.");
            return;
        }

        debugLogService.LogService("Stock", "Starting stock processing.");
        debugLogService.LogService(
            "Stock",
            $"Unlimited={settings.UnlimitedStock}, Randomize={settings.RandomizeStockAvailable}, OutOfStockChance={settings.OutOfStockChance}%");

        var itemsToRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var random = new Random();

        var rootItemsSeen = 0;
        var unlimitedApplied = 0;
        var randomRemoved = 0;
        var missingUpd = 0;

        foreach (var item in context.Assort.Items)
        {
            if (!string.Equals(item.ParentId, "hideout", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            rootItemsSeen++;

            if (settings.RandomizeStockAvailable && random.Next(0, 100) < settings.OutOfStockChance)
            {
                itemsToRemove.Add(item.Id);
                randomRemoved++;
                continue;
            }

            if (item.Upd == null)
            {
                missingUpd++;
                continue;
            }

            if (settings.UnlimitedStock)
            {
                item.Upd.UnlimitedCount = true;
                item.Upd.StackObjectsCount = 999999;

                if (item.Upd.BuyRestrictionMax > 0)
                {
                    item.Upd.BuyRestrictionMax = 9999;
                    item.Upd.BuyRestrictionCurrent = 0;
                }

                unlimitedApplied++;
            }
            else
            {
                item.Upd.UnlimitedCount = false;
            }
        }

        var removedFromAssort = 0;

        if (itemsToRemove.Count > 0)
        {
            var beforeCount = context.Assort.Items.Count;

            context.Assort.Items.RemoveAll(x =>
                itemsToRemove.Contains(x.Id) || itemsToRemove.Contains(x.ParentId));

            removedFromAssort = beforeCount - context.Assort.Items.Count;

            foreach (var itemId in itemsToRemove)
            {
                context.Assort.BarterScheme.Remove(itemId);
                context.Assort.LoyalLevelItems.Remove(itemId);
            }
        }

        debugLogService.LogService(
            "Stock",
            $"Completed stock processing. RootItemsSeen={rootItemsSeen}, UnlimitedApplied={unlimitedApplied}, RandomRootItemsRemoved={randomRemoved}, MissingUpd={missingUpd}, RemovedFromAssort={removedFromAssort}, RemainingItems={context.Assort.Items.Count}");
    }
}