using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using WTTServerCommonLib.Models;

namespace CommonLibExtended.Items.Services.ItemHelpers;

[Injectable]
public sealed class TraderOfferHelper(DebugLogHelper debugLogHelper)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;

    public bool ApplyPresetOffer(
        TraderAssort assort,
        string offerId,
        List<Item> builtItems,
        List<ConfigBarterScheme>? barters,
        TraderOfferSettings? settings,
        string sourceName,
        string context)
    {
        if (builtItems == null || builtItems.Count == 0)
        {
            _debugLogHelper.LogError(sourceName, $"{context}: preset builtItems is null or empty");
            return false;
        }

        var rootExists = builtItems.Any(x => x.Id.ToString().Equals(offerId, StringComparison.OrdinalIgnoreCase));
        if (!rootExists)
        {
            _debugLogHelper.LogError(
                sourceName,
                $"{context}: preset root item {offerId} was not found in builtItems");
            return false;
        }

        return ApplyOfferInternal(
            assort,
            offerId,
            builtItems,
            barters,
            settings,
            sourceName,
            context,
            offerKind: "preset");
    }

    public bool ApplySingleItemOffer(
        TraderAssort assort,
        string offerId,
        List<Item> rootItem,
        List<ConfigBarterScheme>? barters,
        TraderOfferSettings? settings,
        string sourceName,
        string context)
    {
        if (rootItem == null || rootItem.Count == 0)
        {
            _debugLogHelper.LogError(sourceName, $"{context}: rootItem is null or empty");
            return false;
        }

        if (!rootItem[0].Id.ToString().Equals(offerId, StringComparison.OrdinalIgnoreCase))
        {
            _debugLogHelper.LogError(
                sourceName,
                $"{context}: single-item root id {rootItem[0].Id} does not match offerId {offerId}");
            return false;
        }

        return ApplyOfferInternal(
            assort,
            offerId,
            rootItem,
            barters,
            settings,
            sourceName,
            context,
            offerKind: "single");
    }

    private bool ApplyOfferInternal(
        TraderAssort assort,
        string offerId,
        List<Item> builtItems,
        List<ConfigBarterScheme>? barters,
        TraderOfferSettings? settings,
        string sourceName,
        string context,
        string offerKind)
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

        if (builtItems == null || builtItems.Count == 0)
        {
            _debugLogHelper.LogError(sourceName, $"{context}: builtItems is null or empty");
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
                $"{context}: added {offerKind} assort item Id={item.Id}, ParentId={item.ParentId}, Template={item.Template}, SlotId={item.SlotId}");
        }

        ApplyBarterScheme(assort, offerId, barters, sourceName, context);
        ApplyLoyalLevel(assort, offerId, settings, sourceName, context);
        ApplyRootItemSettings(assort, offerId, settings, sourceName, context);

        _debugLogHelper.LogService(
            sourceName,
            $"{context}: DEBUG OFFER START\n" +
            $"OfferId={offerId}\n" +
            $"ITEMS:\n{DebugDumpHelper.DumpItems(builtItems)}\n" +
            $"BARTERS:\n{DebugDumpHelper.DumpItems(barters)}\n" +
            $"SETTINGS:\n{DebugDumpHelper.DumpItems(settings)}\n" +
            $"--- END OFFER DEBUG ---");

        _debugLogHelper.LogService(
            sourceName,
            $"{context}: applied {offerKind} offer for offerId={offerId}, itemCount={builtItems.Count}");

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