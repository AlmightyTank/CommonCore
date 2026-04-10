using CommonLibExtended.Helpers;
using CommonLibExtended.Traders.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace CommonLibExtended.Traders.Services;

[Injectable]
public sealed class TraderPricingService(
    DatabaseService databaseService,
    DebugLogHelper logger)
{
    private readonly DatabaseService _db = databaseService;
    private readonly DebugLogHelper _log = logger;

    public double GetItemPrice(string tpl, CustomTraderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(tpl))
        {
            return 0;
        }

        var handbook = GetHandbookPrice(tpl);

        if (!settings.UseFleaPricing)
        {
            if (handbook > 0)
            {
                return ApplyBase(handbook, settings);
            }

            return GetFallbackCategoryRarityPrice(tpl, settings);
        }

        var flea = GetFleaPrice(tpl);

        if (handbook <= 0 && flea <= 0)
        {
            return GetFallbackCategoryRarityPrice(tpl, settings);
        }

        if (flea <= 0)
        {
            return ApplyBase(handbook, settings);
        }

        if (handbook <= 0)
        {
            return ApplyBase(flea, settings);
        }

        var blended =
            (handbook * settings.HandbookWeight) +
            (flea * settings.FleaWeight);

        return ApplyBase(blended, settings);
    }

    public double GetWeaponBuildPrice(string rootId, TraderAssort assort, CustomTraderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(rootId) || assort?.Items == null)
        {
            return 0;
        }

        var rootItem = assort.Items.FirstOrDefault(x => x != null && x.Id == rootId);
        if (rootItem == null)
        {
            return 0;
        }

        var total = 0.0;
        var stack = new Stack<string>();
        stack.Push(rootId);

        while (stack.Count > 0)
        {
            var currentId = stack.Pop();

            var item = assort.Items.FirstOrDefault(x => x != null && x.Id == currentId);
            if (item == null)
            {
                continue;
            }

            var price = GetItemPrice(item.Template, settings);

            if (currentId == rootId)
            {
                price *= GetRootWeaponMultiplier(item.Template, settings);
                total += price;
            }
            else
            {
                if (!ShouldCountAttachment(item.Template, settings))
                {
                    foreach (var child in assort.Items.Where(x => x != null && x.ParentId == currentId))
                    {
                        stack.Push(child.Id);
                    }

                    continue;
                }

                if (settings.UseAttachmentWeighting)
                {
                    price *= GetAttachmentMultiplier(item.Template, settings);
                }

                if (price >= settings.MinAttachmentPrice)
                {
                    total += price;
                }
            }

            foreach (var child in assort.Items.Where(x => x != null && x.ParentId == currentId))
            {
                stack.Push(child.Id);
            }
        }

        return Math.Round(total);
    }

    private double GetRootWeaponMultiplier(string tpl, CustomTraderSettings settings)
    {
        var templates = _db.GetTables()?.Templates?.Items;
        if (templates == null || !templates.TryGetValue(tpl, out var itemTemplate) || itemTemplate == null)
        {
            return settings.WeaponBasePriceMultiplier;
        }

        var weapClassProp = itemTemplate.Properties?.GetType().GetProperty("weapClass");
        var weapClass = weapClassProp?.GetValue(itemTemplate.Properties)?.ToString()?.ToLowerInvariant();

        if (weapClass == "pistol" || weapClass == "revolver")
        {
            return settings.PistolBasePriceMultiplier;
        }

        return settings.WeaponBasePriceMultiplier;
    }

    private bool ShouldCountAttachment(string tpl, CustomTraderSettings settings)
    {
        if (!settings.CountOnlyWeaponRelevantAttachments)
        {
            return true;
        }

        var templates = _db.GetTables()?.Templates?.Items;
        if (templates == null || !templates.TryGetValue(tpl, out var itemTemplate) || itemTemplate == null)
        {
            return false;
        }

        var parent = itemTemplate.Parent.ToString() ?? string.Empty;

        return settings.WeaponRelevantAttachmentParents != null &&
               settings.WeaponRelevantAttachmentParents.Contains(parent, StringComparer.OrdinalIgnoreCase);
    }

    public double GetFallbackCategoryRarityPrice(string tpl, CustomTraderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(tpl))
        {
            return 0;
        }

        var templates = _db.GetTables()?.Templates?.Items;
        if (templates == null || !templates.TryGetValue(tpl, out var itemTemplate) || itemTemplate == null)
        {
            return settings.BasePriceFloor;
        }

        var categoryBase = GetCategoryBasePrice(itemTemplate.Parent.ToString(), settings);
        var rarityMultiplier = GetRarityMultiplier(itemTemplate, settings);
        var estimated = categoryBase * rarityMultiplier;

        _log.LogService(
            nameof(TraderPricingService),
            $"Fallback pricing used for tpl={tpl}, categoryBase={categoryBase}, rarityMultiplier={rarityMultiplier}, estimated={estimated}",
            nameof(GetFallbackCategoryRarityPrice));

        return ApplyBase(estimated, settings);
    }

    private static double ApplyBase(double price, CustomTraderSettings settings)
    {
        if (price <= 0)
        {
            price = settings.BasePriceFloor;
        }

        return Math.Max(settings.BasePriceFloor, Math.Round(price * settings.BasePriceMultiplier));
    }

    private double GetHandbookPrice(string tpl)
    {
        var handbook = _db.GetTables()?.Templates?.Handbook?.Items;
        if (handbook == null)
        {
            return 0;
        }

        var entry = handbook.FirstOrDefault(x => x != null && x.Id == tpl);
        return entry?.Price ?? 0;
    }

    private double GetFleaPrice(string tpl)
    {
        var prices = _db.GetTables()?.Templates?.Prices;
        if (prices != null && prices.TryGetValue(tpl, out var price))
        {
            return price;
        }

        return 0;
    }

    private double GetAttachmentMultiplier(string tpl, CustomTraderSettings settings)
    {
        var templates = _db.GetTables()?.Templates?.Items;
        if (templates == null || !templates.TryGetValue(tpl, out var itemTemplate) || itemTemplate == null)
        {
            return 1.0;
        }

        var parent = itemTemplate.Parent.ToString() ?? string.Empty;

        if (settings.AttachmentCategoryMultipliers != null &&
            settings.AttachmentCategoryMultipliers.TryGetValue(parent, out var configured))
        {
            return configured;
        }

        return 1.0;
    }

    private double GetCategoryBasePrice(string? parentId, CustomTraderSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(parentId) &&
            settings.CategoryBasePrices != null &&
            settings.CategoryBasePrices.TryGetValue(parentId, out var configured))
        {
            return configured;
        }

        return parentId switch
        {
            "5422acb9af1c889c16000029" => 45000, // weapons
            "5485a8684bdc2da71d8b4567" => 800,   // ammo
            "5448e54d4bdc2dcc718b4568" => 65000, // armor
            "5a341c4086f77401f2541505" => 50000, // helmets
            "55818add4bdc2d5b648b456f" => 22000, // optics
            "55818a684bdc2ddd698b456d" => 9000,  // grips
            "550aa4bf4bdc2dd6348b456b" => 18000, // muzzle
            "543be5dd4bdc2deb348b4569" => 40000, // keys
            "5448f3a14bdc2d27728b4569" => 9000,  // meds
            "57864a66245977548f04a81f" => 7000,  // barter loot
            _ => 10000
        };
    }

    private double GetRarityMultiplier(dynamic itemTemplate, CustomTraderSettings settings)
    {
        try
        {
            var props = itemTemplate._props;
            if (props == null)
            {
                return 1.0;
            }

            var rarityProp = props.GetType().GetProperty("RarityPvE");
            if (rarityProp != null)
            {
                var rarity = rarityProp.GetValue(props)?.ToString()?.ToLowerInvariant();

                if (!string.IsNullOrWhiteSpace(rarity) && settings.RarityMultipliers != null)
                {
                    if (settings.RarityMultipliers.TryGetValue(rarity, out double configured))
                    {
                        return configured;
                    }
                }

                return rarity switch
                {
                    "common" => 1.0,
                    "uncommon" => 1.15,
                    "rare" => 1.35,
                    "superrare" => 1.6,
                    _ => 1.0
                };
            }

            var tpl = itemTemplate._id?.ToString();
            var handbookPrice = GetHandbookPrice(tpl);

            if (handbookPrice >= 150000) return 1.6;
            if (handbookPrice >= 80000) return 1.35;
            if (handbookPrice >= 30000) return 1.15;
        }
        catch
        {
            // Ignore and use default
        }

        return 1.0;
    }
}