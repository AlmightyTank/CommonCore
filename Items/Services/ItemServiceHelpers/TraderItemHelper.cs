using CommonCore.Constants;
using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using static CommonCore.Items.Models.ItemCreationRequest;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable]
public sealed class TraderItemHelper(
    ISptLogger<TraderItemHelper> logger,
    CommonCoreDb db,
    CommonCoreSettings settings)
{
    private const string HideoutId = "hideout";
    private const string RubTpl = "5449016a4bdc2d6f028b456f";
    private const string UsdTpl = "5696686a4bdc2da3298b456a";
    private const string EurTpl = "569668774bdc2da2298b4568";

    public void Process(ItemCreationRequest request)
    {
        try
        {
            if (!ShouldProcess(request))
            {
                return;
            }

            var traderId = ResolveTargetTraderId(request);
            if (string.IsNullOrWhiteSpace(traderId))
            {
                logger.Warning($"[TraderItem] Could not resolve trader for item {request.NewId}");
                return;
            }

            if (!db.Traders.TryGetValue(traderId, out var trader))
            {
                logger.Warning($"[TraderItem] Trader not found in DB: {traderId}");
                return;
            }

            trader.Assort.Items ??= [];
            trader.Assort.BarterScheme ??= [];
            trader.Assort.LoyalLevelItems ??= [];

            var offers = ResolveTraderOffers(request);
            if (offers.Count == 0)
            {
                logger.Warning($"[TraderItem] No trader offers resolved for item {request.NewId}");
                return;
            }

            foreach (var (assortKey, scheme) in offers)
            {
                if (scheme == null)
                {
                    logger.Warning($"[TraderItem] Null scheme for item {request.NewId}, skipping");
                    continue;
                }

                MongoId assortId;
                if (!string.IsNullOrWhiteSpace(assortKey) && assortKey.IsValidMongoId())
                {
                    assortId = assortKey;
                }
                else
                {
                    assortId = GenerateValidAssortId(request.NewId);
                    logger.Warning($"[TraderItem] Invalid assort id '{assortKey}' for {request.NewId}, generated {assortId}");
                }

                if (trader.Assort.Items.Any(x => x.Id == assortId))
                {
                    logger.Warning($"[TraderItem] Assort id {assortId} already exists on trader {traderId}, skipping");
                    continue;
                }

                var newItem = new Item
                {
                    Id = assortId,
                    Template = request.NewId,
                    ParentId = HideoutId,
                    SlotId = HideoutId,
                    Upd = new Upd
                    {
                        UnlimitedCount = scheme.ConfigBarterSettings.UnlimitedCount,
                        StackObjectsCount = scheme.ConfigBarterSettings.StackObjectsCount,
                        BuyRestrictionCurrent = 0
                    }
                };

                if (scheme.ConfigBarterSettings.BuyRestrictionMax != null)
                {
                    newItem.Upd.BuyRestrictionMax = scheme.ConfigBarterSettings.BuyRestrictionMax;
                }

                var barterSchemeItems = BuildBarterSchemes(request, scheme);
                if (barterSchemeItems.Count == 0)
                {
                    barterSchemeItems.Add(new BarterScheme
                    {
                        Count = request.HandbookPriceRoubles ?? request.FleaPriceRoubles ?? 1,
                        Template = RubTpl
                    });
                }

                trader.Assort.Items.Add(newItem);
                trader.Assort.BarterScheme[assortId] = [barterSchemeItems];
                trader.Assort.LoyalLevelItems[assortId] = scheme.ConfigBarterSettings.LoyalLevel;

                logger.Debug($"[TraderItem] Added {request.NewId} to trader {traderId} with assort {assortId}");
            }
        }
        catch (Exception ex)
        {
            logger.Critical($"[TraderItem] Error adding {request.NewId} to traders", ex);
        }
    }

    private bool ShouldProcess(ItemCreationRequest request)
    {
        return request.AddToTraders
            || (request.Traders != null && request.Traders.Count > 0)
            || settings.ForceAllItemsToDefaultTrader;
    }

    private string? ResolveTargetTraderId(ItemCreationRequest request)
    {
        if (settings.ForceAllItemsToDefaultTrader)
        {
            logger.Debug($"[TraderItem] ForceAllItemsToDefaultTrader enabled, routing {request.NewId} to {settings.DefaultTraderId}");
            return settings.DefaultTraderId;
        }

        if (!string.IsNullOrWhiteSpace(request.TraderId))
        {
            var explicitTraderId = ResolveTraderId(request.TraderId);
            if (!string.IsNullOrWhiteSpace(explicitTraderId))
            {
                logger.Debug($"[TraderItem] Using explicit trader {explicitTraderId} for {request.NewId}");
                return explicitTraderId;
            }
        }

        var categoryTraderId = ResolveCategoryTrader(request);
        logger.Debug($"[TraderItem] Using category/default trader {categoryTraderId} for {request.NewId}");
        return categoryTraderId;
    }

    private static Dictionary<string, ConfigTraderScheme> ResolveTraderOffers(ItemCreationRequest request)
    {
        if (request.Traders != null && request.Traders.Count > 0)
        {
            return request.Traders;
        }

        return BuildDefaultTraderOffers(request);
    }

    private static Dictionary<string, ConfigTraderScheme> BuildDefaultTraderOffers(ItemCreationRequest request)
    {
        var assortId = !string.IsNullOrWhiteSpace(request.AssortId) && request.AssortId.IsValidMongoId()
            ? request.AssortId
            : GenerateValidAssortId(request.NewId).ToString();

        var scheme = new ConfigTraderScheme
        {
            ConfigBarterSettings = new ConfigBarterSettings
            {
                LoyalLevel = request.TraderLoyaltyLevel ?? 1,
                UnlimitedCount = true,
                StackObjectsCount = 999999,
                BuyRestrictionMax = request.BuyRestrictionMax ?? 100
            },
            Barters =
            [
                new ConfigBarterScheme
                {
                    Template = "RUB",
                    Count = request.HandbookPriceRoubles ?? request.FleaPriceRoubles ?? 1
                }
            ]
        };

        return new Dictionary<string, ConfigTraderScheme>(StringComparer.OrdinalIgnoreCase)
        {
            [assortId] = scheme
        };
    }

    private static List<BarterScheme> BuildBarterSchemes(ItemCreationRequest request, ConfigTraderScheme scheme)
    {
        var result = new List<BarterScheme>();

        if (scheme.Barters == null)
        {
            return result;
        }

        foreach (var barterConfig in scheme.Barters)
        {
            if (barterConfig == null || string.IsNullOrWhiteSpace(barterConfig.Template))
            {
                continue;
            }

            var barter = new BarterScheme
            {
                Count = barterConfig.Count,
                Template = ResolveBarterTemplate(barterConfig.Template)
            };

            if (barterConfig.Level != null)
            {
                barter.Level = barterConfig.Level;
            }

            if (barterConfig.OnlyFunctional != null)
            {
                barter.OnlyFunctional = barterConfig.OnlyFunctional;
            }

            if (barterConfig.Side != null)
            {
                barter.Side = barterConfig.Side;
            }

            if (barterConfig.SptQuestLocked != null)
            {
                barter.SptQuestLocked = barterConfig.SptQuestLocked;
            }

            result.Add(barter);
        }

        return result;
    }

    private static MongoId ResolveBarterTemplate(string template)
    {
        if (template.Equals("RUB", StringComparison.OrdinalIgnoreCase))
        {
            return RubTpl;
        }

        if (template.Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            return UsdTpl;
        }

        if (template.Equals("EUR", StringComparison.OrdinalIgnoreCase))
        {
            return EurTpl;
        }

        return ItemTplResolver.ResolveId(template);
    }

    private string ResolveCategoryTrader(ItemCreationRequest request)
    {
        var parentId = request.ParentId ?? string.Empty;

        if (parentId.Equals("5485a8684bdc2da71d8b4567", StringComparison.OrdinalIgnoreCase))
        {
            return settings.DefaultTraderId;
        }

        if (parentId.Equals("5448e54d4bdc2dcc718b4568", StringComparison.OrdinalIgnoreCase) ||
            parentId.Equals("5448e5284bdc2dcb718b4567", StringComparison.OrdinalIgnoreCase) ||
            parentId.Equals("57bef4c42459772e8d35a53b", StringComparison.OrdinalIgnoreCase))
        {
            return settings.DefaultTraderId;
        }

        if (parentId.Equals("5447b5f14bdc2d61278b4567", StringComparison.OrdinalIgnoreCase) ||
            parentId.Equals("5447b5fc4bdc2d87278b4567", StringComparison.OrdinalIgnoreCase) ||
            parentId.Equals("555ef6e44bdc2de9068b457e", StringComparison.OrdinalIgnoreCase))
        {
            return settings.DefaultTraderId;
        }

        return settings.DefaultTraderId;
    }

    private static string? ResolveTraderId(string traderKey)
    {
        if (string.IsNullOrWhiteSpace(traderKey))
        {
            return null;
        }

        if (ItemMaps.TraderMap.TryGetValue(traderKey.ToLower(), out var traderId))
        {
            return traderId;
        }

        if (traderKey.IsValidMongoId())
        {
            return traderKey;
        }

        return null;
    }

    private static MongoId GenerateValidAssortId(string itemId)
    {
        var chars = itemId.ToCharArray();
        chars[0] = chars[0] != '3' ? '3' : '4';
        return new string(chars);
    }
}