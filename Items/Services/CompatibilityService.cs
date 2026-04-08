using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonLibExtended.Items.Services;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public sealed class CompatibilityService(
    ISptLogger<CompatibilityService> logger,
    DatabaseService databaseService)
{
    private const string AllSlotsKey = "AllSlots";
    private const string ConflictingItemsKey = "ConflictingItems";
    private const string AmmoKey = "Ammo";

    private readonly ISptLogger<CompatibilityService> _logger = logger;
    private readonly DatabaseService _databaseService = databaseService;

    private readonly Dictionary<string, Dictionary<MongoId, List<MongoId>>> _compatibilityMapping = [];

    public void Initialize()
    {
        _compatibilityMapping.Clear();
        _compatibilityMapping[AllSlotsKey] = [];
        _compatibilityMapping[ConflictingItemsKey] = [];
        _compatibilityMapping[AmmoKey] = [];
    }

    public void AddAmmoClone(MongoId itemToAdd, MongoId cloneId)
    {
        AddMapping(AllSlotsKey, cloneId, itemToAdd);
        AddMapping(AmmoKey, cloneId, itemToAdd);
    }

    public void AddScriptedConflicts(MongoId itemId, ConflictingInfos[] infos)
    {
        if (!_databaseService.GetItems().TryGetValue(itemId, out var item) || item.Properties?.ConflictingItems == null)
        {
            _logger.Error($"Item {itemId} not found or has no conflicting items list");
            return;
        }

        foreach (var info in infos)
        {
            if (!TryGetSlot(info.Id, info.TgtSlotName, out var sourceSlot) || sourceSlot?.Properties?.Filters == null)
            {
                _logger.Error($"Source slot {info.TgtSlotName} not found on {info.Id}");
                continue;
            }

            var sourceFilter = sourceSlot.Properties.Filters.ElementAtOrDefault(0)?.Filter;
            if (sourceFilter == null)
            {
                _logger.Error($"Source slot {info.TgtSlotName} on {info.Id} has no filter");
                continue;
            }

            var filter = new HashSet<MongoId>(sourceFilter);

            if (info.ItemsAddToSlot?.Length > 0)
            {
                foreach (var tpl in info.ItemsAddToSlot)
                {
                    filter.Add(tpl);
                }
            }

            item.Properties.ConflictingItems.UnionWith(filter);
        }
    }

    public void ProcessCompatibilityInfo()
    {
        foreach (var (_, item) in _databaseService.GetItems())
        {
            if (item.Properties == null)
            {
                continue;
            }

            if (item.Properties.ConflictingItems?.Count > 0)
            {
                ApplyCompatibility(ConflictingItemsKey, item.Properties.ConflictingItems);
            }

            if (item.Properties.Slots?.Any() == true)
            {
                foreach (var slot in item.Properties.Slots)
                {
                    var filter = slot.Properties?.Filters?.ElementAtOrDefault(0)?.Filter;
                    if (filter == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(slot.Name))
                    {
                        ApplyCompatibility(slot.Name, filter);
                    }

                    ApplyCompatibility(AllSlotsKey, filter);
                }
            }

            if (item.Properties.Chambers?.Any() == true)
            {
                foreach (var chamber in item.Properties.Chambers)
                {
                    var filter = chamber.Properties?.Filters?.ElementAtOrDefault(0)?.Filter;
                    if (filter != null)
                    {
                        ApplyCompatibility(AmmoKey, filter);
                    }
                }
            }

            if (item.Properties.Cartridges?.Any() == true)
            {
                foreach (var cartridge in item.Properties.Cartridges)
                {
                    var filter = cartridge.Properties?.Filters?.ElementAtOrDefault(0)?.Filter;
                    if (filter != null)
                    {
                        ApplyCompatibility(AmmoKey, filter);
                    }
                }
            }
        }
    }

    private void AddMapping(string key, MongoId cloneId, MongoId itemToAdd)
    {
        if (!_compatibilityMapping.TryGetValue(key, out var inner))
        {
            inner = [];
            _compatibilityMapping[key] = inner;
        }

        if (!inner.TryGetValue(cloneId, out var list))
        {
            list = [];
            inner[cloneId] = list;
        }

        if (!list.Contains(itemToAdd))
        {
            list.Add(itemToAdd);
        }
    }

    private void ApplyCompatibility(string key, HashSet<MongoId> filter)
    {
        if (!_compatibilityMapping.TryGetValue(key, out var mapping))
        {
            return;
        }

        foreach (var pair in mapping)
        {
            if (!filter.Contains(pair.Key))
            {
                continue;
            }

            filter.UnionWith(pair.Value);
        }
    }

    private bool TryGetSlot(MongoId itemId, string slotName, out Slot? slot)
    {
        slot = null;

        if (!_databaseService.GetItems().TryGetValue(itemId, out var item) || item.Properties?.Slots == null)
        {
            return false;
        }

        slot = item.Properties.Slots.FirstOrDefault(x =>
            string.Equals(x.Name, slotName, StringComparison.OrdinalIgnoreCase));

        return slot != null;
    }
}