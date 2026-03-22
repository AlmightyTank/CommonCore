using CommonCore.Core;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using static CommonCore.Items.Models.ItemCreationRequest;

namespace CommonCore.Helpers;

[Injectable(InjectionType.Singleton)]
public sealed class TraderRoutingHelper(CommonCoreSettings settings)
{
    public string ResolveTargetTraderId(ItemCreationRequest request)
    {
        if (settings.ForceAllItemsToDefaultTrader)
        {
            return settings.DefaultTraderId;
        }

        if (!string.IsNullOrWhiteSpace(request.TraderId))
        {
            return request.TraderId;
        }

        return ResolveCategoryTrader(request);
    }

    public Dictionary<string, ConfigTraderScheme> ResolveTraderOffer(ItemCreationRequest request)
    {
        if (request.Traders != null)
        {
            return request.Traders;
        }

        return BuildDefaultTraderOffer(request);
    }

    public MongoId ResolveAssortId(ItemCreationRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.AssortId))
        {
            return request.AssortId;
        }

        return GenerateValidAssortId(request.NewId);
    }

    public bool ShouldProcess(ItemCreationRequest request)
    {
        return request.AddToTraders
            || request.Traders != null
            || settings.ForceAllItemsToDefaultTrader;
    }

    private string ResolveCategoryTrader(ItemCreationRequest request)
    {
        var parentId = request.ParentId ?? string.Empty;

        // ammo
        if (parentId.Equals("5485a8684bdc2da71d8b4567", StringComparison.OrdinalIgnoreCase))
        {
            return settings.DefaultTraderId;
        }

        // armor / rigs / equipment
        if (parentId.Equals("5448e54d4bdc2dcc718b4568", StringComparison.OrdinalIgnoreCase) ||
            parentId.Equals("5448e5284bdc2dcb718b4567", StringComparison.OrdinalIgnoreCase) ||
            parentId.Equals("57bef4c42459772e8d35a53b", StringComparison.OrdinalIgnoreCase))
        {
            return settings.DefaultTraderId;
        }

        // weapons / weapon parts
        if (parentId.Equals("5447b5f14bdc2d61278b4567", StringComparison.OrdinalIgnoreCase) ||
            parentId.Equals("5447b5fc4bdc2d87278b4567", StringComparison.OrdinalIgnoreCase) ||
            parentId.Equals("555ef6e44bdc2de9068b457e", StringComparison.OrdinalIgnoreCase))
        {
            return settings.DefaultTraderId;
        }

        return settings.DefaultTraderId;
    }

    private static Dictionary<string, ConfigTraderScheme> BuildDefaultTraderOffer(ItemCreationRequest request)
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

    private static MongoId GenerateValidAssortId(string itemId)
    {
        var chars = itemId.ToCharArray();
        chars[0] = chars[0] != '3' ? '3' : '4';
        return new string(chars);
    }
}