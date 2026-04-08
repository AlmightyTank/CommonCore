using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace CommonLibExtended.Items.Services.ItemHelpers.Helpers;

[Injectable]
public sealed class StandardTraderItemBuildHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    IdGenerationHelper idGenerationHelper)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly DatabaseService _databaseService = databaseService;
    private readonly IdGenerationHelper _idGenerationHelper = idGenerationHelper;

    public List<Item> BuildItemsForTrader(
        ItemModificationRequest request,
        string assortId,
        TraderOfferSettings? settings = null)
    {
        var results = new List<Item>();

        if (request == null)
        {
            _debugLogHelper.LogError("StandardTraderItemBuildHelper", "request is null");
            return results;
        }

        if (string.IsNullOrWhiteSpace(request.ItemId))
        {
            _debugLogHelper.LogError("StandardTraderItemBuildHelper", "request.ItemId is null or empty");
            return results;
        }

        if (string.IsNullOrWhiteSpace(assortId))
        {
            _debugLogHelper.LogError(
                "StandardTraderItemBuildHelper",
                $"assortId is null or empty for item {request.ItemId}");
            return results;
        }

        var rootItem = CreateRootItem(request.ItemId, assortId, settings);
        results.Add(rootItem);

        if (!ShouldBuildChildren(request.ItemId))
        {
            _debugLogHelper.LogService(
                "StandardTraderItemBuildHelper",
                $"Built standard single-item trader offer for item={request.ItemId}, assortId={assortId}");

            return results;
        }

        var children = BuildDefaultChildren(rootItem);
        results.AddRange(children);

        _debugLogHelper.LogService(
            "StandardTraderItemBuildHelper",
            $"Built trader offer with children for item={request.ItemId}, assortId={assortId}, childCount={children.Count}");

        return results;
    }

    private bool ShouldBuildChildren(string templateId)
    {
        var items = _databaseService.GetItems();
        if (!items.TryGetValue(new MongoId(templateId), out var itemTemplate) || itemTemplate?.Properties == null)
        {
            _debugLogHelper.LogError(
                "StandardTraderItemBuildHelper",
                $"Item template {templateId} not found in database");

            return false;
        }

        var slots = itemTemplate.Properties.Slots;
        if (slots == null)
        {
            return false;
        }

        // Detect if the item has slots that likely need actual children on trader offers.
        // You can tighten or loosen this logic later.
        foreach (var slot in slots)
        {
            if (slot?.Properties?.Filters == null)
            {
                continue;
            }

            foreach (var filter in slot.Properties.Filters)
            {
                if (filter == null)
                {
                    continue;
                }

                // Plate slot / locked slot / single fixed filter are all good signals
                // that this item should get attached children in the trader offer tree.
                if (filter.Plate.HasValue && !filter.Plate.Value.IsEmpty
                    || (filter.Locked ?? false)
                    || filter.Filter != null && filter.Filter.Count == 1)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private List<Item> BuildDefaultChildren(Item rootItem)
    {
        var results = new List<Item>();

        var items = _databaseService.GetItems();
        if (!items.TryGetValue(rootItem.Template, out var rootTemplate) || rootTemplate?.Properties?.Slots == null)
        {
            return results;
        }

        foreach (var slot in rootTemplate.Properties.Slots)
        {
            if (slot?.Properties?.Filters == null)
            {
                continue;
            }

            foreach (var filter in slot.Properties.Filters)
            {
                if (filter == null)
                {
                    continue;
                }

                // Prefer Plate if present, otherwise use the single allowed item
                var childTemplateId =
                    filter.Plate.HasValue && !filter.Plate.Value.IsEmpty
                        ? filter.Plate.Value.ToString()
                        : filter.Filter != null && filter.Filter.Count == 1
                            ? filter.Filter.First().ToString()
                            : null;

                if (string.IsNullOrWhiteSpace(childTemplateId))
                {
                    continue;
                }

                var childId = _idGenerationHelper.GenerateMongoId();

                var child = new Item
                {
                    Id = new MongoId(childId),
                    ParentId = rootItem.Id,
                    SlotId = slot.Name,
                    Template = new MongoId(childTemplateId)
                };

                results.Add(child);

                _debugLogHelper.LogService(
                    "StandardTraderItemBuildHelper",
                    $"Added child item template={childTemplateId} into slot={slot.Name} on root={rootItem.Template}");
            }
        }

        return results;
    }

    private static Item CreateRootItem(
        string templateId,
        string assortId,
        TraderOfferSettings? settings)
    {
        return new Item
        {
            Id = new MongoId(assortId),
            Template = new MongoId(templateId),
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                StackObjectsCount = settings?.StackObjectsCount > 0
                    ? settings.StackObjectsCount
                    : null,

                UnlimitedCount = settings?.UnlimitedCount,

                BuyRestrictionMax = settings?.UnlimitedCount == true
                    ? 0
                    : Math.Max(0, settings?.BuyRestrictionMax ?? 0)
            }
        };
    }
}