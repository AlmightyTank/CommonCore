namespace CommonCore.Traders.Service.Sub;

using CommonCore.Helpers;
using CommonCore.Traders.Models;
using SPTarkov.DI.Annotations;

[Injectable]
public sealed class TraderPricingService(
    CoreDebugLogHelper debugLogService)
{
    public void Apply(TraderLoadContext context)
    {
        var multiplier = context.Settings.PriceMultiplier;

        if (Math.Abs(multiplier - 1.0) <= 0.001)
        {
            debugLogService.LogService("Pricing", "Skipped pricing changes. Multiplier is standard price (1.0).");
            return;
        }

        var mode = multiplier > 1.0 ? "increase" : "decrease";
        var updatedCount = 0;

        debugLogService.LogService(
            "Pricing",
            $"Applying price {mode}. Multiplier={multiplier}");

        foreach (var schemeEntry in context.Assort.BarterScheme)
        {
            foreach (var schemeGroup in schemeEntry.Value)
            {
                foreach (var requirement in schemeGroup)
                {
                    if (!requirement.Count.HasValue)
                    {
                        continue;
                    }

                    requirement.Count = Math.Round(requirement.Count.Value * multiplier);
                    updatedCount++;
                }
            }
        }

        debugLogService.LogService(
            "Pricing",
            $"Finished pricing update. UpdatedRequirements={updatedCount}, Multiplier={multiplier}");
    }
}