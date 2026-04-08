using CommonLibExtended.Helpers;
using CommonLibExtended.Traders.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using System.Globalization;

namespace CommonLibExtended.Traders.Helpers;

[Injectable]
public sealed class CustomTraderSettingsHelper(DebugLogHelper debugLogHelper)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;

    public void ApplyBaseSettings(TraderBase traderBase, CustomTraderSettings settings)
    {
        if (traderBase == null)
        {
            return;
        }

        settings.Validate(traderBase.Id);

        traderBase.UnlockedByDefault = settings.UnlockedByDefault;

        if (traderBase.LoyaltyLevels != null && traderBase.LoyaltyLevels.Count > 0)
        {
            traderBase.LoyaltyLevels[0].MinLevel = settings.MinLevel;

            foreach (var level in traderBase.LoyaltyLevels)
            {
                TrySetInsuranceCoefficient(level, settings.InsurancePriceCoef, traderBase.Id);
            }
        }

        if (traderBase.Repair != null)
        {
            traderBase.Repair.Quality = settings.RepairQuality;
        }

        if (settings.DebugLogging)
        {
            _debugLogHelper.LogService(
                "CustomTraderSettingsHelper",
                $"Applied base settings to trader {traderBase.Id}");
        }
    }

    public void ApplyFleaSettings(RagfairConfig ragfairConfig, TraderBase traderBase, CustomTraderSettings settings)
    {
        if (ragfairConfig == null || traderBase == null)
        {
            return;
        }

        if (settings.AddTraderToFleaMarket)
        {
            ragfairConfig.Traders[traderBase.Id] = true;
        }
        else
        {
            ragfairConfig.Traders.Remove(traderBase.Id);
        }

        if (settings.DebugLogging)
        {
            _debugLogHelper.LogService(
                "CustomTraderSettingsHelper",
                $"Applied flea market setting for trader {traderBase.Id}: {settings.AddTraderToFleaMarket}");
        }
    }

    public int ApplyRefreshSettings(TraderConfig traderConfig, TraderBase traderBase, CustomTraderSettings settings)
    {
        if (traderConfig == null || traderBase == null)
        {
            return 0;
        }

        var random = new Random();
        var refreshTime = random.Next(settings.TraderRefreshMin, settings.TraderRefreshMax + 1);

        traderConfig.UpdateTime.RemoveAll(x => x.TraderId == traderBase.Id);
        traderConfig.UpdateTime.Add(new UpdateTime
        {
            TraderId = traderBase.Id,
            Seconds = new MinMax<int>(refreshTime, refreshTime)
        });

        traderBase.NextResupply = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + refreshTime);

        if (settings.DebugLogging)
        {
            _debugLogHelper.LogService(
                "CustomTraderSettingsHelper",
                $"Applied refresh settings to trader {traderBase.Id}: {refreshTime}s");
        }

        return refreshTime;
    }

    public void ApplyAssortSettings(TraderAssort assort, CustomTraderSettings settings)
    {
        if (assort?.Items == null)
        {
            return;
        }

        var random = new Random();
        var itemsToRemove = new List<string>();

        foreach (var item in assort.Items)
        {
            if (item == null || item.ParentId != "hideout" || item.Upd == null)
            {
                continue;
            }

            if (settings.RandomizeStockAvailable &&
                random.Next(0, 100) < settings.OutOfStockChance)
            {
                itemsToRemove.Add(item.Id);
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
            }
            else
            {
                item.Upd.UnlimitedCount = false;

                if (item.Upd.StackObjectsCount <= 0)
                {
                    item.Upd.StackObjectsCount = 100;
                }
            }
        }

        if (itemsToRemove.Count > 0)
        {
            assort.Items.RemoveAll(x => itemsToRemove.Contains(x.Id) || itemsToRemove.Contains(x.ParentId));

            foreach (var id in itemsToRemove)
            {
                assort.BarterScheme.Remove(id);
                assort.LoyalLevelItems.Remove(id);
            }
        }

        ApplyPriceMultiplier(assort, settings);

        if (settings.DebugLogging)
        {
            _debugLogHelper.LogService(
                "CustomTraderSettingsHelper",
                $"Applied assort settings. Removed {itemsToRemove.Count} out-of-stock item(s)");
        }
    }

    private void ApplyPriceMultiplier(TraderAssort assort, CustomTraderSettings settings)
    {
        if (Math.Abs(settings.PriceMultiplier - 1.0) < 0.001)
        {
            return;
        }

        foreach (var (_, schemeList) in assort.BarterScheme)
        {
            foreach (var schemeSubList in schemeList)
            {
                foreach (var component in schemeSubList)
                {
                    if (component.Count.HasValue)
                    {
                        component.Count = Math.Round(component.Count.Value * settings.PriceMultiplier);
                    }
                }
            }
        }

        if (settings.DebugLogging)
        {
            _debugLogHelper.LogService(
                "CustomTraderSettingsHelper",
                $"Applied price multiplier: {settings.PriceMultiplier.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    private void TrySetInsuranceCoefficient(object loyaltyLevel, double insurancePriceCoef, string traderId)
    {
        try
        {
            var prop = loyaltyLevel.GetType().GetProperty("InsurancePriceCoefficient");
            if (prop == null || !prop.CanWrite)
            {
                return;
            }

            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var converted = Convert.ChangeType(insurancePriceCoef, targetType, CultureInfo.InvariantCulture);
            prop.SetValue(loyaltyLevel, converted);
        }
        catch (Exception ex)
        {
            _debugLogHelper.LogError(
                "CustomTraderSettingsHelper",
                $"Failed setting insurance coefficient for trader {traderId}: {ex.Message}");
        }
    }
}