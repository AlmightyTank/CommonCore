using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Constants;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using CommonCore.Items.Models;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable]
public class TraderItemHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db)
{
    public void Process(ItemCreationRequest request)
    {
        try
        {
            if (request.Traders == null || request.Traders.Count == 0)
            {
                debugLogHelper.LogError("TraderItemHelper", $"No trader entries for item {request.NewId}");
                return;
            }

            var traders = db.Traders;

            foreach (var (traderKey, schemes) in request.Traders)
            {
                MongoId actualTraderId;

                if (ItemMaps.TraderMap.TryGetValue(traderKey.ToLower(), out var traderId))
                {
                    actualTraderId = traderId;
                }
                else if (traderKey.IsValidMongoId())
                {
                    actualTraderId = traderKey;
                }
                else
                {
                    debugLogHelper.LogError("TraderItemHelper", $"Invalid trader key: {traderKey}");
                    continue;
                }

                if (!traders.TryGetValue(actualTraderId, out var trader))
                {
                    debugLogHelper.LogError("TraderItemHelper", $"Trader not found in DB: ({actualTraderId})");
                    continue;
                }

                trader.Assort.Items ??= [];
                trader.Assort.BarterScheme ??= [];
                trader.Assort.LoyalLevelItems ??= [];

                foreach (var (schemeKey, scheme) in schemes)
                {
                    var newItem = new Item
                    {
                        Id = schemeKey,
                        Template = request.NewId,
                        ParentId = "hideout",
                        SlotId = "hideout",
                        Upd = new Upd
                        {
                            UnlimitedCount = scheme.ConfigBarterSettings.UnlimitedCount,
                            StackObjectsCount = scheme.ConfigBarterSettings.StackObjectsCount
                        }
                    };

                    if (scheme.ConfigBarterSettings.BuyRestrictionMax != null)
                    {
                        newItem.Upd.BuyRestrictionMax = scheme.ConfigBarterSettings.BuyRestrictionMax;
                    }

                    trader.Assort.Items.Add(newItem);

                    if (!trader.Assort.BarterScheme.TryGetValue(schemeKey, out var barterOptions))
                    {
                        barterOptions = [];
                        trader.Assort.BarterScheme[schemeKey] = barterOptions;
                    }

                    var barterSchemeItems = new List<BarterScheme>();

                    foreach (var b in scheme.Barters)
                    {
                        if (string.IsNullOrWhiteSpace(b.Template))
                        {
                            continue;
                        }

                        var barter = new BarterScheme
                        {
                            Count = b.Count,
                            Template = ItemTplResolver.ResolveId(b.Template)
                        };

                        if (b.Level != null)
                        {
                            barter.Level = b.Level;
                        }

                        if (b.OnlyFunctional != null)
                        {
                            barter.OnlyFunctional = b.OnlyFunctional;
                        }

                        if (b.Side != null)
                        {
                            barter.Side = b.Side;
                        }

                        if (b.SptQuestLocked != null)
                        {
                            barter.SptQuestLocked = b.SptQuestLocked;
                        }

                        barterSchemeItems.Add(barter);
                    }

                    if (barterSchemeItems.Count > 0)
                    {
                        barterOptions.Add(barterSchemeItems);
                    }

                    trader.Assort.LoyalLevelItems[schemeKey] = scheme.ConfigBarterSettings.LoyalLevel;
                }
            }
        }
        catch (Exception)
        {
            debugLogHelper.LogError("TraderItemHelper", $"Error adding {request.NewId} to traders");
        }
    }
}