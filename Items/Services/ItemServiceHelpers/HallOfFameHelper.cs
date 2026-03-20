using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable]
public sealed class HallOfFameHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db)
{
    private static readonly string[] ValidTypes =
    [
        "dogtag",
        "smallTrophies",
        "bigTrophies"
    ];

    private static readonly string[] HallItemIds =
    [
        "63dbd45917fff4dee40fe16e", // Level 1
        "65424185a57eea37ed6562e9", // Level 2
        "6542435ea57eea37ed6562f0"  // Level 3
    ];

    public void Process(ItemCreationRequest request)
    {
        var filterTypes = GetValidFilterTypes(request);
        if (filterTypes.Count == 0)
        {
            debugLogHelper.LogError("HallOfFameHelper", $"No valid slot types for {request.NewId}");
            return;
        }

        foreach (var hallId in HallItemIds)
        {
            if (!db.Items.TryGetValue(hallId, out var hallItem) || hallItem.Properties?.Slots == null)
            {
                continue;
            }

            AddItemToHallSlots(request.NewId, hallItem, filterTypes);
        }
    }

    private static HashSet<string> GetValidFilterTypes(ItemCreationRequest request)
    {
        var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (request.HallOfFameSlots == null)
        {
            return types;
        }

        foreach (var slot in request.HallOfFameSlots)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                continue;
            }

            if (slot.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var type in ValidTypes)
                {
                    types.Add(type);
                }

                continue;
            }

            foreach (var type in ValidTypes)
            {
                if (slot.Equals(type, StringComparison.OrdinalIgnoreCase))
                {
                    types.Add(type);
                }
            }
        }

        return types;
    }

    private void AddItemToHallSlots(string itemId, TemplateItem hallItem, HashSet<string> filterTypes)
    {
        if (hallItem.Properties?.Slots == null)
        {
            return;
        }

        foreach (var slot in hallItem.Properties.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Name) || slot.Properties?.Filters == null)
            {
                continue;
            }

            var slotType = GetMatchingSlotType(slot.Name);
            if (slotType == null || !filterTypes.Contains(slotType))
            {
                continue;
            }

            AddItemToFilters(itemId, slot, hallItem.Name);
        }
    }

    private static string? GetMatchingSlotType(string slotName)
    {
        foreach (var type in ValidTypes)
        {
            if (slotName.StartsWith(type, StringComparison.OrdinalIgnoreCase))
            {
                return type;
            }
        }

        return null;
    }

    private void AddItemToFilters(string itemId, Slot slot, string? hallName)
    {
        if (slot.Properties?.Filters == null)
        {
            return;
        }

        foreach (var filter in slot.Properties.Filters)
        {
            filter.Filter ??= new HashSet<MongoId>();

            if (filter.Filter.Add(itemId))
            {
                debugLogHelper.LogService("HallOfFameHelper", $"Added {itemId} to slot '{slot.Name}' in {hallName}");
            }
            debugLogHelper.LogService("HallOfFameHelper", $"{itemId} already in slot '{slot.Name}'");
        }
    }
}