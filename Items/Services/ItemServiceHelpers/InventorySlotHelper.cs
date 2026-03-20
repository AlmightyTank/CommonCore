using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable]
public class InventorySlotHelper(CommonCoreDb db, CoreDebugLogHelper debugLogHelper)
{
    public void Process(ItemCreationRequest request)
    {
        if (request.AddToInventorySlots == null)
            return;

        const string pmcInventoryTemplateId = "55d7217a4bdc2d86028b456d";

        var items = db.Items;
        var defaultInventorySlots = items[pmcInventoryTemplateId].Properties?.Slots;
        if (defaultInventorySlots == null)
            return;

        var allowedSlots = request.AddToInventorySlots
            .Select(slot => slot.ToLower())
            .ToList();

        foreach (var slot in defaultInventorySlots)
        {
            var filtersList = slot.Properties?.Filters?.ToList();
            if (filtersList == null || filtersList.Count == 0)
                continue;

            var slotNameLower = slot.Name?.ToLower();
            if (slotNameLower == null)
                continue;

            if (allowedSlots.Contains(slotNameLower))
            {
                var firstFilter = filtersList.FirstOrDefault();
                if (firstFilter?.Filter == null)
                    continue;

                if (firstFilter.Filter.Add(request.NewId))
                    debugLogHelper.LogService("InventorySlotHelper", $"Added {request.NewId} to inventory slot '{slot.Name}'");
            }
        }
    }
}