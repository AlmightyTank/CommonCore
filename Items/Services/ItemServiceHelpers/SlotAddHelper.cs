using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public class SlotAddHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db)
{
    public void Process(ItemCreationRequest request)
    {
        if (!request.AddSlot)
        {
            return;
        }

        if (request.SlotsToAdd == null || request.SlotsToAdd.Length == 0)
        {
            debugLogHelper.LogError("SlotAddHelper", $"Invalid SlotsToAdd for {request.NewId}");
            return;
        }

        if (!db.Items.TryGetValue(request.NewId, out var itemTemplate))
        {
            debugLogHelper.LogError("SlotAddHelper", $"Item {request.NewId} not found.");
            return;
        }

        itemTemplate.Properties ??= new TemplateItemProperties();

        var slots = itemTemplate.Properties.Slots?.ToList() ?? new List<Slot>();

        foreach (var slotToAdd in request.SlotsToAdd)
        {
            if (slotToAdd == null || string.IsNullOrWhiteSpace(slotToAdd.Name))
            {
                debugLogHelper.LogError("SlotAddHelper", $"Skipping invalid slot on {request.NewId}");
                continue;
            }

            var exists = slots.Any(s =>
                s.Name != null &&
                s.Name.Equals(slotToAdd.Name, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                debugLogHelper.LogError("SlotAddHelper", $"Slot '{slotToAdd.Name}' already exists on {request.NewId}");
                continue;
            }

            slots.Add(slotToAdd);
            debugLogHelper.LogService("SlotAddHelper", $"Added slot '{slotToAdd.Name}' to {request.NewId}");
        }

        itemTemplate.Properties.Slots = slots;
    }
}