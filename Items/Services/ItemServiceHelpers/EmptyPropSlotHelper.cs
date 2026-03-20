using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable]
public class EmptyPropSlotHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db
)
{
    public void Process(ItemCreationRequest request)
    {
        if (!request.AddToEmptyPropSlots)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.NewId))
        {
            debugLogHelper.LogError("EmptyPropSlotHelper", $"NewId is null or empty.");
            return;
        }

        var emptyPropSlot = request.EmptyPropSlot as EmptyPropSlotConfig;
        if (emptyPropSlot == null)
        {
            debugLogHelper.LogError("EmptyPropSlotHelper", $"EmptyPropSlot missing for {request.NewId}");
            return;
        }

        var itemToAddTo = emptyPropSlot.ItemToAddTo;
        var slotName = emptyPropSlot.ModSlot;

        if (string.IsNullOrWhiteSpace(itemToAddTo) || string.IsNullOrWhiteSpace(slotName))
        {
            debugLogHelper.LogError("EmptyPropSlotHelper", $"Invalid EmptyPropSlot data for {request.NewId}");
            return;
        }

        var database = db.Items;

        if (!database.TryGetValue(itemToAddTo, out var targetItem))
        {
            debugLogHelper.LogError("EmptyPropSlotHelper", $"Target item '{itemToAddTo}' not found for {request.NewId}");
            return;
        }

        if (targetItem.Properties == null)
        {
            debugLogHelper.LogError("EmptyPropSlotHelper", $"Target item '{itemToAddTo}' has null properties");
            return;
        }

        var slots = targetItem.Properties.Slots?.ToList() ?? [];

        if (slots.Count != 0)
        {
            debugLogHelper.LogService("EmptyPropSlotHelper", $"Target item '{itemToAddTo}' already has slots, skipping for {request.NewId}");
            return;
        }

        var newSlot = new Slot
        {
            Id = new MongoId(),
            MergeSlotWithChildren = false,
            Name = slotName,
            Parent = itemToAddTo,
            Properties = new SlotProperties
            {
                Filters = new List<SlotFilter>
                {
                    new SlotFilter
                    {
                        Filter = new HashSet<MongoId>
                        {
                            new MongoId(request.NewId)
                        }
                    }
                }
            },
            Prototype = new MongoId(),
            Required = false
        };

        slots.Add(newSlot);
        targetItem.Properties.Slots = slots;

        debugLogHelper.LogService("EmptyPropSlotHelper", $"Added empty prop slot '{slotName}' to '{itemToAddTo}' for {request.NewId}");
    }
}