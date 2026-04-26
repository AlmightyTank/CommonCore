using CommonLibExtended.Helpers;
using CommonLibExtended.Traders.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using System.Globalization;

namespace CommonLibExtended.Traders.TraderHelpers;

[Injectable]
public sealed class CustomTraderSettingsHelper(
    DatabaseService databaseService,
    DebugLogHelper logger,
    ConfigServer configServer)
{
    private const string RubTpl = "5449016a4bdc2d6f028b456f";
    private const string UsdTpl = "5696686a4bdc2da3298b456a";
    private const string EurTpl = "569668774bdc2da2298b4568";

    private readonly DebugLogHelper _log = logger;
    private readonly DatabaseService _databaseService = databaseService;
    private readonly ConfigServer _configServer = configServer;

    public void ApplyPricing(TraderAssort assort, CustomTraderSettings settings)
    {
        if (assort?.Items == null || assort.BarterScheme == null || assort.LoyalLevelItems == null)
        {
            return;
        }

        var appliedManualOverrides = 0;
        var appliedCurrencyPrices = 0;
        var skippedOffers = 0;
        var skippedBecauseMissingScheme = 0;
        var skippedBecauseMissingLoyalty = 0;
        var skippedBecauseInvalidOverride = 0;
        var skippedBecauseNoAssortPrice = 0;

        var targetCurrencyTpl = GetCurrencyTpl(settings.Currency);

        foreach (var item in assort.Items)
        {
            if (item?.ParentId != "hideout")
            {
                continue;
            }

            if (!assort.BarterScheme.TryGetValue(item.Id, out var existingSchemeLists) ||
                existingSchemeLists == null ||
                existingSchemeLists.Count == 0)
            {
                skippedOffers++;
                skippedBecauseMissingScheme++;
                continue;
            }

            if (!assort.LoyalLevelItems.ContainsKey(item.Id))
            {
                skippedOffers++;
                skippedBecauseMissingLoyalty++;
                continue;
            }

            if (settings.AssortBarterOverrides != null &&
                settings.AssortBarterOverrides.Enabled &&
                settings.AssortBarterOverrides.ParsedOverrides.TryGetValue(item.Id, out var assortOverride) &&
                assortOverride != null &&
                assortOverride.Enabled)
            {
                var barterSettings = assortOverride.ToBarterSchemeList();

                if (barterSettings.Count == 0)
                {
                    skippedOffers++;
                    skippedBecauseInvalidOverride++;

                    _log.LogError(
                        nameof(CustomTraderSettingsHelper),
                        $"Barter override for assortId={item.Id} had no valid barterSettings",
                        nameof(ApplyPricing));

                    continue;
                }

                assort.BarterScheme[item.Id] = [barterSettings];
                appliedManualOverrides++;

                if (settings.DebugLogging)
                {
                    foreach (var entry in barterSettings)
                    {
                        _log.LogService(
                            nameof(CustomTraderSettingsHelper),
                            $"Applied manual barter override for assortId={item.Id}: _tpl={entry.Template}, count={entry.Count}",
                            nameof(ApplyPricing));
                    }
                }

                continue;
            }

            var rubBasePrice = GetCurrentAssortPriceInRub(assort, item.Id);
            if (rubBasePrice <= 0)
            {
                skippedOffers++;
                skippedBecauseNoAssortPrice++;
                continue;
            }

            var multipliedRubPrice = Math.Max(1, Math.Round(rubBasePrice * settings.PriceMultiplier));

            if (!settings.EnableCurrency)
            {
                // 🔹 Keep original barter, do NOT convert
                if (settings.DebugLogging)
                {
                    _log.LogService(
                        nameof(CustomTraderSettingsHelper),
                        $"Skipped currency conversion for assortId={item.Id}, EnableCurrency=false, basePrice={rubBasePrice}, multiplied={multipliedRubPrice}",
                        nameof(ApplyPricing));
                }

                ScaleExistingBarter(assort, item.Id, settings.PriceMultiplier);
                continue;
            }

            // 🔹 Convert to selected currency
            var finalCurrencyAmount = ConvertRubToTargetCurrencyAmount(multipliedRubPrice, targetCurrencyTpl);

            assort.BarterScheme[item.Id] =
            [
                [
                    new()
                    {
                        Template = targetCurrencyTpl,
                        Count = finalCurrencyAmount
                    }
                ]
            ];

            appliedCurrencyPrices++;

            if (settings.DebugLogging)
            {
                _log.LogService(
                    nameof(CustomTraderSettingsHelper),
                    $"Applied currency price for assortId={item.Id}: currency={settings.Currency}, _tpl={targetCurrencyTpl}, rubBasePrice={rubBasePrice}, multipliedRubPrice={multipliedRubPrice}, finalCurrencyAmount={finalCurrencyAmount}",
                    nameof(ApplyPricing));
            }
        }

        if (settings.DebugLogging)
        {
            _log.LogService(
                nameof(CustomTraderSettingsHelper),
                $"ApplyPricing → appliedManualOverrides={appliedManualOverrides}, appliedCurrencyPrices={appliedCurrencyPrices}, skippedOffers={skippedOffers}, skippedBecauseMissingScheme={skippedBecauseMissingScheme}, skippedBecauseMissingLoyalty={skippedBecauseMissingLoyalty}, skippedBecauseInvalidOverride={skippedBecauseInvalidOverride}, skippedBecauseNoAssortPrice={skippedBecauseNoAssortPrice}",
                nameof(ApplyPricing));
        }
    }

    private void ScaleExistingBarter(TraderAssort assort, string assortId, double multiplier)
    {
        if (!assort.BarterScheme.TryGetValue(assortId, out var schemeLists) ||
            schemeLists == null ||
            schemeLists.Count == 0)
        {
            return;
        }

        foreach (var scheme in schemeLists)
        {
            foreach (var entry in scheme)
            {
                if (entry?.Count.HasValue == true)
                {
                    entry.Count = Math.Max(1, Math.Round(entry.Count.Value * multiplier));
                }
            }
        }
    }

    private double GetCurrentAssortPriceInRub(TraderAssort assort, string assortId)
    {
        if (!assort.BarterScheme.TryGetValue(assortId, out var schemeLists) ||
            schemeLists == null ||
            schemeLists.Count == 0)
        {
            return 0;
        }

        var scheme = schemeLists.FirstOrDefault();
        if (scheme == null || scheme.Count == 0)
        {
            return 0;
        }

        double totalRubValue = 0;

        foreach (var entry in scheme)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Template) || entry.Count.HasValue != true || entry.Count.Value <= 0)
            {
                continue;
            }

            var entryRubValue = GetItemValueInRub(entry.Template);
            if (entryRubValue <= 0)
            {
                continue;
            }

            totalRubValue += entryRubValue * entry.Count.Value;
        }

        return Math.Round(totalRubValue);
    }

    private double GetItemValueInRub(string tpl)
    {
        if (string.IsNullOrWhiteSpace(tpl))
        {
            return 0;
        }

        if (tpl == RubTpl)
        {
            return 1;
        }

        var prices = _databaseService.GetTables()?.Templates?.Prices;
        if (prices != null && prices.TryGetValue(tpl, out var fleaPrice) && fleaPrice > 0)
        {
            return fleaPrice;
        }

        var handbook = _databaseService.GetTables()?.Templates?.Handbook?.Items;
        var handbookEntry = handbook?.FirstOrDefault(x => x != null && x.Id == tpl);
        if (handbookEntry?.Price > 0)
        {
            return handbookEntry.Price ?? 0;
        }

        return 0;
    }

    private double ConvertRubToTargetCurrencyAmount(double rubAmount, string targetCurrencyTpl)
    {
        if (rubAmount <= 0)
        {
            return 0;
        }

        if (targetCurrencyTpl == RubTpl)
        {
            return Math.Max(1, Math.Round(rubAmount));
        }

        var targetCurrencyRubValue = GetItemValueInRub(targetCurrencyTpl);
        if (targetCurrencyRubValue <= 0)
        {
            return Math.Max(1, Math.Round(rubAmount));
        }

        return Math.Max(1, Math.Round(rubAmount / targetCurrencyRubValue));
    }

    private static string GetCurrencyTpl(string currency)
    {
        return (currency ?? "RUB").Trim().ToUpperInvariant() switch
        {
            "USD" => UsdTpl,
            "EUR" => EurTpl,
            _ => RubTpl
        };
    }

    public void ApplyAssortSettings(TraderAssort assort, CustomTraderSettings settings)
    {
        if (assort?.Items == null)
        {
            return;
        }

        var random = new Random();
        var toRemove = new List<string>();

        foreach (var item in assort.Items)
        {
            if (item?.ParentId != "hideout" || item.Upd == null)
            {
                continue;
            }

            if (settings.RandomizeStockAvailable &&
                random.Next(0, 100) < settings.OutOfStockChance)
            {
                toRemove.Add(item.Id);
                continue;
            }

            if (settings.UnlimitedStock)
            {
                item.Upd.UnlimitedCount = true;
                item.Upd.StackObjectsCount = 999999;
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

        var initialRootCount = assort.Items.Count(x => x.ParentId == "hideout");
        var removedRootCount = toRemove.Count;

        if (removedRootCount > 0)
        {
            assort.Items.RemoveAll(x => toRemove.Contains(x.Id) || toRemove.Contains(x.ParentId));

            foreach (var id in toRemove)
            {
                assort.BarterScheme.Remove(id);
                assort.LoyalLevelItems.Remove(id);
            }
        }

        var percent = initialRootCount > 0
            ? removedRootCount / (double)initialRootCount * 100
            : 0;

        if (settings.DebugLogging)
        {
            _log.LogService(
                nameof(CustomTraderSettingsHelper),
                $"Assort cleanup: {removedRootCount}/{initialRootCount} removed ({percent:F1}%)",
                nameof(ApplyAssortSettings));
        }
    }

    public void ApplyBaseSettings(TraderBase traderBase, CustomTraderSettings settings)
    {
        if (traderBase == null)
        {
            return;
        }

        settings.Validate(traderBase.Id);

        traderBase.UnlockedByDefault = settings.UnlockedByDefault;

        if (traderBase.LoyaltyLevels?.Count > 0)
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
            _log.LogService(
                nameof(CustomTraderSettingsHelper),
                $"BaseSettings applied → Trader={traderBase.Id}, MinLevel={settings.MinLevel}, RepairQ={settings.RepairQuality}, InsuranceCoef={settings.InsurancePriceCoef}",
                nameof(ApplyBaseSettings));
        }
    }

    private void TrySetInsuranceCoefficient(object loyaltyLevel, double insurancePriceCoef, string traderId)
    {
        if (loyaltyLevel == null)
        {
            return;
        }

        try
        {
            var prop = loyaltyLevel.GetType().GetProperty("InsurancePriceCoefficient");

            if (prop == null || !prop.CanWrite)
            {
                return;
            }

            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var value = Convert.ChangeType(insurancePriceCoef, targetType, CultureInfo.InvariantCulture);

            prop.SetValue(loyaltyLevel, value);
        }
        catch (Exception ex)
        {
            _log.LogError(
                nameof(CustomTraderSettingsHelper),
                $"Insurance coef failed for trader {traderId}: {ex.Message}",
                nameof(TrySetInsuranceCoefficient));
        }
    }

    public void ApplyFleaSettings(TraderBase traderBase, CustomTraderSettings settings)
    {
        var ragfairConfig = _configServer.GetConfig<RagfairConfig>();

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
            _log.LogService(
                nameof(CustomTraderSettingsHelper),
                $"FleaSettings → Trader={traderBase.Id}, Enabled={settings.AddTraderToFleaMarket}",
                nameof(ApplyFleaSettings));
        }
    }

    public int ApplyRefreshSettings(TraderBase traderBase, CustomTraderSettings settings)
    {
        var traderConfig = _configServer.GetConfig<TraderConfig>();

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
            _log.LogService(
                nameof(CustomTraderSettingsHelper),
                $"RefreshSettings → Trader={traderBase.Id}, Refresh={refreshTime}s",
                nameof(ApplyRefreshSettings));
        }

        return refreshTime;
    }
}