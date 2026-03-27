using CommonCore.Core;
using CommonCore.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Cloners;

namespace CommonCore.Helpers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public sealed class SlotCloneHelper(
    CoreDebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    ICloner cloner)
{
    public void Process(CommonCoreItemRequest request)
    {

        if (!request.Config.CopySlot == true)
        {
            return;
        }

        if (request.Config.CopySlots == null || request.Config.CopySlots.Count == 0)
        {
            debugLogHelper.LogError("SlotCloneHelper", $"Invalid CopySlotsInfo for {request.ItemId}");
            return;
        }

        if (!databaseService.GetItems().TryGetValue(request.ItemId, out var targetItem))
        {
            debugLogHelper.LogError("SlotCloneHelper", $"Target item {request.ItemId} not found.");
            return;
        }

        targetItem.Properties ??= new TemplateItemProperties();

        var existingSlots = targetItem.Properties.Slots?.ToList() ?? new List<Slot>();
        var newSlots = new List<Slot>();

        foreach (var copyInfo in request.Config.CopySlots)
        {
            if (copyInfo == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(copyInfo.Id))
            {
                debugLogHelper.LogError("SlotCloneHelper", $"Source item id missing for {request.ItemId}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(copyInfo.NewSlotName))
            {
                debugLogHelper.LogError("SlotCloneHelper", $"NewSlotName missing for {request.ItemId}");
                continue;
            }

            var targetSlotName = copyInfo.TgtSlotName ?? copyInfo.NewSlotName;

            if (!TryGetSlotByName(copyInfo.Id, targetSlotName, out var sourceSlot) || sourceSlot == null)
            {
                debugLogHelper.LogError("SlotCloneHelper", $"Slot '{targetSlotName}' of id '{copyInfo.Id}' not found when adding to {request.ItemId}");
                continue;
            }

            if (sourceSlot.Properties?.Filters == null)
            {
                debugLogHelper.LogError("SlotCloneHelper", $"Slot '{targetSlotName}' of id '{copyInfo.Id}' is invalid when adding to {request.ItemId}");
                continue;
            }

            var filters = cloner.Clone(sourceSlot.Properties.Filters);
            if (filters == null)
            {
                debugLogHelper.LogError("SlotCloneHelper", $"Failed to clone filters for slot '{targetSlotName}' from '{copyInfo.Id}'");
                continue;
            }

            if (copyInfo.ItemsAddToSlot != null && copyInfo.ItemsAddToSlot.Length > 0)
            {
                var firstFilter = filters.FirstOrDefault();
                if (firstFilter?.Filter != null)
                {
                    foreach (var tpl in copyInfo.ItemsAddToSlot)
                    {
                        firstFilter.Filter.Add(tpl);
                    }
                }
            }

            var alreadyExists = existingSlots.Any(x =>
                !string.IsNullOrWhiteSpace(x.Name) &&
                x.Name.Equals(copyInfo.NewSlotName, StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
            {
                debugLogHelper.LogError("SlotCloneHelper", $"Slot '{copyInfo.NewSlotName}' already exists on {request.ItemId}, skipping.");
                continue;
            }

            var newSlot = new Slot
            {
                Name = copyInfo.NewSlotName,
                Id = MongoId.Empty(),
                Parent = request.ItemId,
                Properties = new SlotProperties
                {
                    Filters = filters
                },
                Required = copyInfo.Required ?? sourceSlot.Required,
                MergeSlotWithChildren = sourceSlot.MergeSlotWithChildren,
                Prototype = sourceSlot.Prototype
            };

            newSlots.Add(newSlot);
            debugLogHelper.LogService("SlotCloneHelper", $"Copied slot '{targetSlotName}' from '{copyInfo.Id}' to '{copyInfo.NewSlotName}' on {request.ItemId}");
        }

        existingSlots.AddRange(newSlots);
        targetItem.Properties.Slots = existingSlots;
    }

    private bool TryGetSlotByName(string itemId, string slotName, out Slot? slot)
    {
        slot = null;

        if (!databaseService.GetItems().TryGetValue(itemId, out var item))
        {
            return false;
        }

        var slots = item.Properties?.Slots;
        if (slots == null)
        {
            return false;
        }

        foreach (var s in slots)
        {
            if (!string.IsNullOrWhiteSpace(s.Name) &&
                s.Name.Equals(slotName, StringComparison.OrdinalIgnoreCase))
            {
                slot = s;
                return true;
            }
        }

        return false;
    }
}