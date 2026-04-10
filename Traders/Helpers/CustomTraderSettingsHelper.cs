using CommonLibExtended.Helpers;
using CommonLibExtended.Traders.Models;
using CommonLibExtended.Traders.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using System.Globalization;

namespace CommonLibExtended.Traders.Helpers;

[Injectable]
public sealed class CustomTraderSettingsHelper(
    TraderPricingService pricingService,
    DatabaseService databaseService,
    DebugLogHelper logger,
    ConfigServer configServer)
{
    private readonly TraderPricingService _pricing = pricingService;
    private readonly DebugLogHelper _log = logger;
    private readonly DatabaseService _databaseService = databaseService;
    private readonly ConfigServer _configServer = configServer;

    public void ApplyPricing(TraderAssort assort, CustomTraderSettings settings)
    {
        if (assort?.Items == null || assort.BarterScheme == null || assort.LoyalLevelItems == null)
            return;

        var repricedOffers = 0;
        var rebuiltBarters = 0;
        var skippedOffers = 0;
        var howManyWereSkippedWerentPriced = 0;

        foreach (var item in assort.Items)
        {
            if (item?.ParentId != "hideout")
                continue;

            if (!assort.BarterScheme.TryGetValue(item.Id, out var schemeLists) || schemeLists == null || schemeLists.Count == 0)
            {
                skippedOffers++;
                continue;
            }

            if (!assort.LoyalLevelItems.ContainsKey(item.Id))
            {
                skippedOffers++;
                continue;
            }

            var targetPrice = settings.UseAttachmentPricing
                ? _pricing.GetWeaponBuildPrice(item.Id, assort, settings)
                : _pricing.GetItemPrice(item.Template, settings);

            _log.LogService(
            nameof(CustomTraderSettingsHelper),
            $"Calculated target price for item {item.Id} (tpl: {item.Template}): {targetPrice}",
            nameof(ApplyPricing));

            if (targetPrice <= 0)
            {
                targetPrice = _pricing.GetFallbackCategoryRarityPrice(item.Template, settings);

                _log.LogService(
                    nameof(CustomTraderSettingsHelper),
                    $"Fallback target price for item {item.Id} (tpl: {item.Template}): {targetPrice}",
                    nameof(ApplyPricing));

                if (targetPrice <= 0)
                {
                    skippedOffers++;
                    howManyWereSkippedWerentPriced++;
                    continue;
                }
            }

            targetPrice = Math.Round(targetPrice * settings.PriceMultiplier);

            var existingScheme = schemeLists.FirstOrDefault();
            if (existingScheme == null || existingScheme.Count == 0)
            {
                skippedOffers++;
                continue;
            }

            if (IsCash(existingScheme))
            {
                RepriceCashScheme(existingScheme, targetPrice);
                repricedOffers++;
                continue;
            }

            if (!settings.RebuildItemBarters)
            {
                skippedOffers++;
                continue;
            }

            var rebuilt = TryBuildBarterSchemeFromTargetPrice(targetPrice, settings);
            if (rebuilt == null || rebuilt.Count == 0)
            {
                skippedOffers++;
                continue;
            }

            assort.BarterScheme[item.Id] = [rebuilt];
            rebuiltBarters++;
        }

        _log.LogService(
            nameof(CustomTraderSettingsHelper),
            $"ApplyPricing → cashRepriced={repricedOffers}, bartersRebuilt={rebuiltBarters}, skipped={skippedOffers}, howManyWereSkippedWerentPriced={howManyWereSkippedWerentPriced}",
            nameof(ApplyPricing));
    }

    private static bool IsCash(List<BarterScheme> scheme)
    {
        foreach (var c in scheme)
        {
            if (c.Template != "5449016a4bdc2d6f028b456f" &&
                c.Template != "5696686a4bdc2da3298b456a" &&
                c.Template != "569668774bdc2da2298b4568")
                return false;
        }
        return true;
    }

    private static void RepriceCashScheme(List<BarterScheme> scheme, double targetPrice)
    {
        if (scheme == null || scheme.Count == 0)
            return;

        foreach (var component in scheme)
        {
            if (component?.Count.HasValue != true)
                continue;

            component.Count = Math.Max(1, Math.Round(targetPrice));
        }
    }

    private List<BarterScheme>? TryBuildBarterSchemeFromTargetPrice(
        double targetPrice,
        CustomTraderSettings settings)
    {
        if (targetPrice <= 0 ||
            settings.PreferredBarterTpls == null ||
            settings.PreferredBarterTpls.Count == 0)
        {
            return null;
        }

        var tolerance = Math.Max(1, targetPrice * settings.BarterValueTolerance);
        var maxComponents = Math.Max(1, settings.MaxBarterComponents);

        var pool = settings.PreferredBarterTpls
            .Where(tpl => !string.IsNullOrWhiteSpace(tpl))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(tpl => new
            {
                Tpl = tpl,
                Price = _pricing.GetItemPrice(tpl, settings)
            })
            .Where(x => x.Price > 0)
            .OrderByDescending(x => x.Price)
            .ToList();

        if (pool.Count == 0)
        {
            return null;
        }

        var scheme = new List<BarterScheme>();
        var remaining = targetPrice;

        foreach (var item in pool)
        {
            if (scheme.Count >= maxComponents || remaining <= tolerance)
            {
                break;
            }

            var count = (int)Math.Floor(remaining / item.Price);
            if (count <= 0)
            {
                continue;
            }

            count = Math.Min(count, 99);

            scheme.Add(new BarterScheme
            {
                Template = item.Tpl,
                Count = count
            });

            remaining -= item.Price * count;
        }

        if (remaining > tolerance)
        {
            var nearest = pool
                .OrderBy(x => Math.Abs(x.Price - remaining))
                .FirstOrDefault();

            if (nearest != null)
            {
                var existing = scheme.FirstOrDefault(x => string.Equals(x.Template, nearest.Tpl, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.Count = (existing.Count ?? 0) + 1;
                }
                else if (scheme.Count < maxComponents)
                {
                    scheme.Add(new BarterScheme
                    {
                        Template = nearest.Tpl,
                        Count = 1
                    });
                }
            }
        }

        if (scheme.Count == 0)
        {
            return null;
        }

        var totalValue = scheme.Sum(x =>
        {
            if (x?.Template == null || x.Count.HasValue != true)
            {
                return 0;
            }

            return _pricing.GetItemPrice(x.Template, settings) * x.Count.Value;
        });

        if (Math.Abs(totalValue - targetPrice) > tolerance)
        {
            return null;
        }

        return scheme;
    }

    public void ApplyAssortSettings(TraderAssort assort, CustomTraderSettings settings)
    {
        if (assort?.Items == null)
            return;

        var random = new Random();
        var toRemove = new List<string>();

        foreach (var item in assort.Items)
        {
            if (item?.ParentId != "hideout" || item.Upd == null)
                continue;

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
                    item.Upd.StackObjectsCount = 100;
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

        var finalRootCount = assort.Items.Count(x => x.ParentId == "hideout");

        var percent = initialRootCount > 0
            ? (removedRootCount / (double)initialRootCount) * 100
            : 0;

        _log.LogService(
            nameof(CustomTraderSettingsHelper),
            $"Assort cleanup: {removedRootCount}/{initialRootCount} removed ({percent:F1}%)",
            nameof(ApplyAssortSettings));
    }

    public void ApplyBaseSettings(TraderBase traderBase, CustomTraderSettings settings)
    {
        if (traderBase == null)
            return;

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

        _log.LogService(
            nameof(CustomTraderSettingsHelper),
            $"BaseSettings applied → Trader={traderBase.Id}, MinLevel={settings.MinLevel}, RepairQ={settings.RepairQuality}, InsuranceCoef={settings.InsurancePriceCoef}",
            nameof(ApplyBaseSettings));
    }

    private void TrySetInsuranceCoefficient(object loyaltyLevel, double insurancePriceCoef, string traderId)
    {
        if (loyaltyLevel == null)
            return;

        try
        {
            var prop = loyaltyLevel.GetType().GetProperty("InsurancePriceCoefficient");

            if (prop == null || !prop.CanWrite)
                return;

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
            return;

        if (settings.AddTraderToFleaMarket)
        {
            ragfairConfig.Traders[traderBase.Id] = true;
        }
        else
        {
            ragfairConfig.Traders.Remove(traderBase.Id);
        }

        _log.LogService(
        nameof(CustomTraderSettingsHelper),
        $"FleaSettings → Trader={traderBase.Id}, Enabled={settings.AddTraderToFleaMarket}",
        nameof(ApplyFleaSettings));
    }

    public int ApplyRefreshSettings(TraderBase traderBase, CustomTraderSettings settings)
    {
        var traderConfig = _configServer.GetConfig<TraderConfig>();

        if (traderConfig== null || traderBase == null)
            return 0;

        var random = new Random();
        var refreshTime = random.Next(settings.TraderRefreshMin, settings.TraderRefreshMax + 1);

        traderConfig.UpdateTime.RemoveAll(x => x.TraderId == traderBase.Id);

        traderConfig.UpdateTime.Add(new UpdateTime
        {
            TraderId = traderBase.Id,
            Seconds = new MinMax<int>(refreshTime, refreshTime)
        });

        traderBase.NextResupply = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + refreshTime);

        _log.LogService(
        nameof(CustomTraderSettingsHelper),
        $"RefreshSettings → Trader={traderBase.Id}, Refresh={refreshTime}s",
        nameof(ApplyRefreshSettings));


        return refreshTime;
    }
}