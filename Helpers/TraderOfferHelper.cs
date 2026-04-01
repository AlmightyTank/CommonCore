using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace CommonLibExtended.Helpers;

[Injectable]
public sealed class TraderOfferHelper(DebugLogHelper debugLogHelper)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;

    public bool ApplyOffer(
        TraderAssort assort,
        string offerId,
        List<Item> builtItems,
        List<ConfigBarterScheme>? barters,
        TraderOfferSettings? settings,
        string sourceName,
        string context)
    {
        if (assort == null)
        {
            _debugLogHelper.LogError(sourceName, $"{context}: assort is null");
            return false;
        }

        if (string.IsNullOrWhiteSpace(offerId))
        {
            _debugLogHelper.LogError(sourceName, $"{context}: offerId is null or empty");
            return false;
        }

        assort.Items ??= [];
        assort.BarterScheme ??= [];
        assort.LoyalLevelItems ??= [];

        var existingIds = new HashSet<string>(
            assort.Items.Select(x => x.Id.ToString()),
            StringComparer.OrdinalIgnoreCase);

        foreach (var item in builtItems)
        {
            var itemId = item.Id.ToString();
            if (existingIds.Contains(itemId))
            {
                continue;
            }

            assort.Items.Add(item);
            existingIds.Add(itemId);

            _debugLogHelper.LogService(
                sourceName,
                $"{context}: added assort item Id={item.Id}, ParentId={item.ParentId}, Template={item.Template}, SlotId={item.SlotId}");
        }

        ApplyBarterScheme(assort, offerId, barters, sourceName, context);
        ApplyLoyalLevel(assort, offerId, settings, sourceName, context);
        ApplyRootItemSettings(assort, offerId, settings, sourceName, context);

        _debugLogHelper.LogService(
            sourceName,
            $"{context}: offer applied for offerId={offerId}, itemCount={builtItems.Count}");

        return true;
    }

    private void ApplyBarterScheme(
        TraderAssort assort,
        string offerId,
        List<ConfigBarterScheme>? barters,
        string sourceName,
        string context)
    {
        if (barters == null || barters.Count == 0)
        {
            _debugLogHelper.LogService(
                sourceName,
                $"{context}: no barter scheme provided for offerId={offerId}");
            return;
        }

        var barterSchemes = barters
            .Select(b => new BarterScheme
            {
                Count = b.Count,
                Template = NormalizeCurrencyTemplate(b.Template)
            })
            .ToList();

        assort.BarterScheme[offerId] = [barterSchemes];

        _debugLogHelper.LogService(
            sourceName,
            $"{context}: applied barter scheme with {barterSchemes.Count} entries for offerId={offerId}");
    }

    private void ApplyLoyalLevel(
        TraderAssort assort,
        string offerId,
        TraderOfferSettings? settings,
        string sourceName,
        string context)
    {
        var loyalLevel = settings?.LoyalLevel ?? 1;
        assort.LoyalLevelItems[offerId] = loyalLevel;

        _debugLogHelper.LogService(
            sourceName,
            $"{context}: applied loyal level {loyalLevel} for offerId={offerId}");
    }

    private void ApplyRootItemSettings(
        TraderAssort assort,
        string offerId,
        TraderOfferSettings? settings,
        string sourceName,
        string context)
    {
        var rootItem = assort.Items.FirstOrDefault(x => x.Id.ToString() == offerId);
        if (rootItem == null)
        {
            _debugLogHelper.LogError(
                sourceName,
                $"{context}: root item not found for offerId={offerId}");
            return;
        }

        rootItem.Upd ??= new Upd();

        if (settings == null)
        {
            return;
        }

        if (settings.StackObjectsCount > 0)
        {
            rootItem.Upd.StackObjectsCount = settings.StackObjectsCount;
        }

        rootItem.Upd.BuyRestrictionMax = settings.UnlimitedCount
            ? 0
            : Math.Max(0, settings.BuyRestrictionMax);

        _debugLogHelper.LogService(
            sourceName,
            $"{context}: applied item settings for offerId={offerId} " +
            $"(UnlimitedCount={settings.UnlimitedCount}, StackObjectsCount={settings.StackObjectsCount}, BuyRestrictionMax={rootItem.Upd.BuyRestrictionMax})");
    }

    private static string NormalizeCurrencyTemplate(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        return template.ToUpperInvariant() switch
        {
            "RUB" => "5449016a4bdc2d6f028b456f",
            "USD" => "5696686a4bdc2da3298b456a",
            "EUR" => "569668774bdc2da2298b4568",
            _ => template
        };
    }
}