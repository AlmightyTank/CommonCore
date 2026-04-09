using CommonLibExtended.Helpers;
using CommonLibExtended.Traders.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Services;
using System.Globalization;

namespace CommonLibExtended.Traders.Helpers;

[Injectable]
public sealed class CustomTraderSettingsHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly DatabaseService _databaseService = databaseService;

    public void ApplyBaseSettings(TraderBase traderBase, CustomTraderSettings settings)
    {
        if (traderBase == null)
        {
            Warn("Trader base was null, skipping ApplyBaseSettings", nameof(ApplyBaseSettings));
            return;
        }

        Debug($"Starting ApplyBaseSettings for trader {traderBase.Id}", nameof(ApplyBaseSettings));

        settings.Validate(traderBase.Id);

        traderBase.UnlockedByDefault = settings.UnlockedByDefault;

        if (traderBase.LoyaltyLevels != null && traderBase.LoyaltyLevels.Count > 0)
        {
            Debug(
                $"Trader {traderBase.Id} has {traderBase.LoyaltyLevels.Count} loyalty levels. Setting LL1 MinLevel={settings.MinLevel}",
                nameof(ApplyBaseSettings));

            traderBase.LoyaltyLevels[0].MinLevel = settings.MinLevel;

            foreach (var level in traderBase.LoyaltyLevels)
            {
                TrySetInsuranceCoefficient(level, settings.InsurancePriceCoef, traderBase.Id);
            }
        }
        else
        {
            Warn($"Trader {traderBase.Id} had no loyalty levels", nameof(ApplyBaseSettings));
        }

        if (traderBase.Repair != null)
        {
            traderBase.Repair.Quality = settings.RepairQuality;
            Debug(
                $"Applied repair quality {settings.RepairQuality.ToString(CultureInfo.InvariantCulture)} to trader {traderBase.Id}",
                nameof(ApplyBaseSettings));
        }
        else
        {
            Warn($"Trader {traderBase.Id} had no repair block", nameof(ApplyBaseSettings));
        }

        Debug($"Finished ApplyBaseSettings for trader {traderBase.Id}", nameof(ApplyBaseSettings));
    }

    public void ApplyFleaSettings(RagfairConfig ragfairConfig, TraderBase traderBase, CustomTraderSettings settings)
    {
        if (ragfairConfig == null || traderBase == null)
        {
            Warn("RagfairConfig or TraderBase was null, skipping ApplyFleaSettings", nameof(ApplyFleaSettings));
            return;
        }

        Debug(
            $"Starting ApplyFleaSettings for trader {traderBase.Id}. AddTraderToFleaMarket={settings.AddTraderToFleaMarket}",
            nameof(ApplyFleaSettings));

        if (settings.AddTraderToFleaMarket)
        {
            ragfairConfig.Traders[traderBase.Id] = true;
            Debug($"Enabled flea market for trader {traderBase.Id}", nameof(ApplyFleaSettings));
        }
        else
        {
            ragfairConfig.Traders.Remove(traderBase.Id);
            Debug($"Removed trader {traderBase.Id} from flea market", nameof(ApplyFleaSettings));
        }

        Debug($"Finished ApplyFleaSettings for trader {traderBase.Id}", nameof(ApplyFleaSettings));
    }

    public int ApplyRefreshSettings(TraderConfig traderConfig, TraderBase traderBase, CustomTraderSettings settings)
    {
        if (traderConfig == null || traderBase == null)
        {
            Warn("TraderConfig or TraderBase was null, skipping ApplyRefreshSettings", nameof(ApplyRefreshSettings));
            return 0;
        }

        Debug(
            $"Starting ApplyRefreshSettings for trader {traderBase.Id}. Range={settings.TraderRefreshMin}-{settings.TraderRefreshMax}",
            nameof(ApplyRefreshSettings));

        var random = new Random();
        var refreshTime = random.Next(settings.TraderRefreshMin, settings.TraderRefreshMax + 1);

        traderConfig.UpdateTime.RemoveAll(x => x.TraderId == traderBase.Id);
        traderConfig.UpdateTime.Add(new UpdateTime
        {
            TraderId = traderBase.Id,
            Seconds = new MinMax<int>(refreshTime, refreshTime)
        });

        traderBase.NextResupply = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + refreshTime);

        Debug($"Applied refresh settings to trader {traderBase.Id}: {refreshTime}s", nameof(ApplyRefreshSettings));
        return refreshTime;
    }

    public void ApplyAssortSettings(TraderAssort assort, CustomTraderSettings settings)
    {
        if (assort?.Items == null)
        {
            Warn("Assort or assort.Items was null, skipping ApplyAssortSettings", nameof(ApplyAssortSettings));
            return;
        }

        Debug(
            $"Starting ApplyAssortSettings. Items={assort.Items.Count}, UseBasePriceGeneration={settings.UseBasePriceGeneration}, PriceMultiplier={settings.PriceMultiplier.ToString(CultureInfo.InvariantCulture)}, UnlimitedStock={settings.UnlimitedStock}, RandomizeStockAvailable={settings.RandomizeStockAvailable}, RepriceCashOffersOnly={settings.RepriceCashOffersOnly}",
            nameof(ApplyAssortSettings));

        var random = new Random();
        var itemsToRemove = new List<string>();

        foreach (var item in assort.Items)
        {
            if (item == null)
            {
                Warn("Encountered null assort item", nameof(ApplyAssortSettings));
                continue;
            }

            Debug(
                $"Inspecting assort item Id={item.Id}, ParentId={item.ParentId}, Template={item.Template}",
                nameof(ApplyAssortSettings));

            if (item.ParentId != "hideout")
            {
                Debug($"Skipping non-root item {item.Id} because ParentId={item.ParentId}", nameof(ApplyAssortSettings));
                continue;
            }

            if (item.Upd == null)
            {
                Warn($"Skipping root item {item.Id} because Upd was null", nameof(ApplyAssortSettings));
                continue;
            }

            if (settings.RandomizeStockAvailable &&
                random.Next(0, 100) < settings.OutOfStockChance)
            {
                itemsToRemove.Add(item.Id);
                Warn($"Marking item {item.Id} for out-of-stock removal", nameof(ApplyAssortSettings));
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

                Debug(
                    $"Applied unlimited stock to item {item.Id}. Stack={item.Upd.StackObjectsCount}, BuyRestrictionMax={item.Upd.BuyRestrictionMax}",
                    nameof(ApplyAssortSettings));
            }
            else
            {
                item.Upd.UnlimitedCount = false;

                if (item.Upd.StackObjectsCount <= 0)
                {
                    item.Upd.StackObjectsCount = 100;
                }

                Debug(
                    $"Applied limited stock to item {item.Id}. Stack={item.Upd.StackObjectsCount}",
                    nameof(ApplyAssortSettings));
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

            Debug($"Removed {itemsToRemove.Count} root item(s) from assort", nameof(ApplyAssortSettings));
        }
        else
        {
            Debug("No items were removed by stock randomization", nameof(ApplyAssortSettings));
        }

        Debug("Calling ApplyBasePricing", nameof(ApplyAssortSettings));
        ApplyBasePricing(assort, settings);

        Debug("Calling ApplyPriceMultiplier", nameof(ApplyAssortSettings));
        ApplyPriceMultiplier(assort, settings);

        Debug(
            $"Finished ApplyAssortSettings. RemainingItems={assort.Items.Count}, BarterSchemes={assort.BarterScheme.Count}, LoyalLevelItems={assort.LoyalLevelItems.Count}",
            nameof(ApplyAssortSettings));
    }

    private void ApplyBasePricing(TraderAssort assort, CustomTraderSettings settings)
    {
        Debug(
            $"Entered ApplyBasePricing. UseBasePriceGeneration={settings.UseBasePriceGeneration}, BasePriceSource={settings.BasePriceSource}, BasePriceMultiplier={settings.BasePriceMultiplier.ToString(CultureInfo.InvariantCulture)}, BasePriceFloor={settings.BasePriceFloor.ToString(CultureInfo.InvariantCulture)}, RepriceCashOffersOnly={settings.RepriceCashOffersOnly}",
            nameof(ApplyBasePricing));

        if (!settings.UseBasePriceGeneration)
        {
            Warn("Base price generation disabled, skipping ApplyBasePricing", nameof(ApplyBasePricing));
            return;
        }

        var processedRootItems = 0;
        var updatedComponents = 0;

        foreach (var item in assort.Items)
        {
            if (item == null)
            {
                Warn("Encountered null assort item during ApplyBasePricing", nameof(ApplyBasePricing));
                continue;
            }

            if (item.ParentId != "hideout")
            {
                Debug($"Skipping non-root item {item.Id} during ApplyBasePricing", nameof(ApplyBasePricing));
                continue;
            }

            processedRootItems++;

            Debug(
                $"Processing root assort item Id={item.Id}, Template={item.Template}, ParentId={item.ParentId}",
                nameof(ApplyBasePricing));

            if (!assort.BarterScheme.TryGetValue(item.Id, out var schemeList) || schemeList == null || schemeList.Count == 0)
            {
                Warn($"No barter scheme found for assort item {item.Id}", nameof(ApplyBasePricing));
                continue;
            }

            Debug($"Found {schemeList.Count} barter scheme list(s) for assort item {item.Id}", nameof(ApplyBasePricing));

            var generatedPrice = GetGeneratedBasePrice(item.Template, settings);
            if (generatedPrice <= 0)
            {
                Warn($"Generated price <= 0 for tpl {item.Template} on assort item {item.Id}", nameof(ApplyBasePricing));
                continue;
            }

            Debug(
                $"Generated base price {generatedPrice.ToString(CultureInfo.InvariantCulture)} for tpl {item.Template} on assort item {item.Id}",
                nameof(ApplyBasePricing));

            for (var i = 0; i < schemeList.Count; i++)
            {
                var schemeSubList = schemeList[i];

                if (schemeSubList == null || schemeSubList.Count == 0)
                {
                    Warn($"Scheme list {i} was null or empty for assort item {item.Id}", nameof(ApplyBasePricing));
                    continue;
                }

                Debug(
                    $"Inspecting scheme list {i} with {schemeSubList.Count} component(s) for assort item {item.Id}",
                    nameof(ApplyBasePricing));

                if (settings.RepriceCashOffersOnly && !IsCashScheme(schemeSubList))
                {
                    Warn($"Skipping non-cash scheme list {i} for assort item {item.Id}", nameof(ApplyBasePricing));
                    continue;
                }

                foreach (var component in schemeSubList)
                {
                    if (component == null)
                    {
                        Warn($"Encountered null barter component for assort item {item.Id}", nameof(ApplyBasePricing));
                        continue;
                    }

                    if (!component.Count.HasValue)
                    {
                        Warn(
                            $"Skipping barter component Template={component.Template} for assort item {item.Id} because Count was null",
                            nameof(ApplyBasePricing));
                        continue;
                    }

                    Debug(
                        $"Updating barter component Template={component.Template} Count={component.Count.Value.ToString(CultureInfo.InvariantCulture)} -> {generatedPrice.ToString(CultureInfo.InvariantCulture)} for assort item {item.Id}",
                        nameof(ApplyBasePricing));

                    component.Count = generatedPrice;
                    updatedComponents++;
                }
            }
        }

        Debug(
            $"Finished ApplyBasePricing. ProcessedRootItems={processedRootItems}, UpdatedComponents={updatedComponents}",
            nameof(ApplyBasePricing));
    }

    private double GetGeneratedBasePrice(string itemTpl, CustomTraderSettings settings)
    {
        Debug(
            $"Calculating base price for tpl={itemTpl} using source={settings.BasePriceSource}",
            nameof(GetGeneratedBasePrice));

        if (string.IsNullOrWhiteSpace(itemTpl))
        {
            Warn("itemTpl was null or whitespace, returning 0", nameof(GetGeneratedBasePrice));
            return 0;
        }

        var source = settings.BasePriceSource?.Trim() ?? "Handbook";
        double basePrice = 0;

        basePrice = source.ToLowerInvariant() switch
        {
            _ => GetHandbookPrice(itemTpl),
        };

        Debug(
            $"Raw base price for tpl={itemTpl} is {basePrice.ToString(CultureInfo.InvariantCulture)}",
            nameof(GetGeneratedBasePrice));

        if (basePrice <= 0)
        {
            Warn(
                $"Base price <= 0 for tpl={itemTpl}, using floor {settings.BasePriceFloor.ToString(CultureInfo.InvariantCulture)}",
                nameof(GetGeneratedBasePrice));

            basePrice = settings.BasePriceFloor;
        }

        if (basePrice <= 0)
        {
            Warn($"Final base price still <= 0 for tpl={itemTpl}, returning 0", nameof(GetGeneratedBasePrice));
            return 0;
        }

        var finalBasePrice = Math.Round(basePrice * settings.BasePriceMultiplier);

        Debug(
            $"Final generated base price for tpl={itemTpl} is {finalBasePrice.ToString(CultureInfo.InvariantCulture)}",
            nameof(GetGeneratedBasePrice));

        return Math.Max(finalBasePrice, settings.BasePriceFloor);
    }

    private double GetHandbookPrice(string itemTpl)
    {
        Debug($"Looking up handbook price for tpl={itemTpl}", nameof(GetHandbookPrice));

        var handbook = _databaseService.GetTables()?.Templates?.Handbook?.Items;
        if (handbook == null)
        {
            Warn("Handbook table was null", nameof(GetHandbookPrice));
            return 0;
        }

        var entry = handbook.FirstOrDefault(x => x != null && x.Id == itemTpl);
        if (entry == null)
        {
            Warn($"No handbook entry found for tpl={itemTpl}", nameof(GetHandbookPrice));
            return 0;
        }

        var price = entry.Price ?? 0;

        Debug(
            $"Handbook price for tpl={itemTpl} is {price.ToString(CultureInfo.InvariantCulture)}",
            nameof(GetHandbookPrice));

        return price;
    }

    private bool IsCashScheme(List<BarterScheme> schemeSubList)
    {
        Debug(
            $"Checking cash scheme with component count={schemeSubList?.Count ?? 0}",
            nameof(IsCashScheme));

        if (schemeSubList == null || schemeSubList.Count == 0)
        {
            Warn("Scheme list was null or empty, not a cash scheme", nameof(IsCashScheme));
            return false;
        }

        foreach (var component in schemeSubList)
        {
            if (component?.Template == null)
            {
                Warn("Scheme rejected as non-cash because a component Template was null", nameof(IsCashScheme));
                return false;
            }

            Debug($"Cash scheme component Template={component.Template}", nameof(IsCashScheme));

            if (component.Template != "5449016a4bdc2d6f028b456f" &&
                component.Template != "5696686a4bdc2da3298b456a" &&
                component.Template != "569668774bdc2da2298b4568")
            {
                Warn(
                    $"Scheme rejected as non-cash because component Template={component.Template}",
                    nameof(IsCashScheme));
                return false;
            }
        }

        Debug("Scheme accepted as cash", nameof(IsCashScheme));
        return true;
    }

    private void ApplyPriceMultiplier(TraderAssort assort, CustomTraderSettings settings)
    {
        Debug(
            $"Entered ApplyPriceMultiplier with multiplier={settings.PriceMultiplier.ToString(CultureInfo.InvariantCulture)}",
            nameof(ApplyPriceMultiplier));

        if (Math.Abs(settings.PriceMultiplier - 1.0) < 0.001)
        {
            Debug("Price multiplier is effectively 1.0, skipping ApplyPriceMultiplier", nameof(ApplyPriceMultiplier));
            return;
        }

        var updatedComponents = 0;

        foreach (var (assortId, schemeList) in assort.BarterScheme)
        {
            Debug(
                $"Applying multiplier to assortId={assortId} with {schemeList.Count} scheme list(s)",
                nameof(ApplyPriceMultiplier));

            foreach (var schemeSubList in schemeList)
            {
                foreach (var component in schemeSubList)
                {
                    if (component?.Count.HasValue == true)
                    {
                        var oldValue = component.Count.Value;
                        var newValue = Math.Round(oldValue * settings.PriceMultiplier);

                        Debug(
                            $"Multiplier update on component Template={component.Template}: {oldValue.ToString(CultureInfo.InvariantCulture)} -> {newValue.ToString(CultureInfo.InvariantCulture)}",
                            nameof(ApplyPriceMultiplier));

                        component.Count = newValue;
                        updatedComponents++;
                    }
                }
            }
        }

        Debug(
            $"Finished ApplyPriceMultiplier. UpdatedComponents={updatedComponents}",
            nameof(ApplyPriceMultiplier));
    }

    private void TrySetInsuranceCoefficient(object loyaltyLevel, double insurancePriceCoef, string traderId)
    {
        try
        {
            var prop = loyaltyLevel.GetType().GetProperty("InsurancePriceCoefficient");
            if (prop == null || !prop.CanWrite)
            {
                Warn(
                    $"InsurancePriceCoefficient property missing or not writable for trader {traderId}",
                    nameof(TrySetInsuranceCoefficient));
                return;
            }

            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var converted = Convert.ChangeType(insurancePriceCoef, targetType, CultureInfo.InvariantCulture);
            prop.SetValue(loyaltyLevel, converted);

            Debug(
                $"Set insurance coefficient for trader {traderId} to {insurancePriceCoef.ToString(CultureInfo.InvariantCulture)}",
                nameof(TrySetInsuranceCoefficient));
        }
        catch (Exception ex)
        {
            _debugLogHelper.LogError(
                nameof(CustomTraderSettingsHelper),
                $"Failed setting insurance coefficient for trader {traderId}: {ex.Message}",
                nameof(TrySetInsuranceCoefficient));
        }
    }

    private void Debug(string message, string functionName)
    {
        _debugLogHelper.LogService(nameof(CustomTraderSettingsHelper), message, functionName);
    }

    private void Warn(string message, string functionName)
    {
        _debugLogHelper.LogWarning(nameof(CustomTraderSettingsHelper), message, functionName);
    }
}