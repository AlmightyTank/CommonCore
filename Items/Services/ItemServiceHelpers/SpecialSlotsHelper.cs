using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable]
public sealed class SpecialSlotsHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db)
{
    public void Process(ItemCreationRequest request)
    {
        if (request.AddToSpecialSlots != true)
        {
            return;
        }

        var pocketIds = new[]
        {
            "627a4e6b255f7527fb05a0f6", // normal pockets
            "65e080be269cbd5c5005e529"  // unheard pockets
        };

        foreach (var pocketsId in pocketIds)
        {
            if (!db.Items.TryGetValue(pocketsId, out var pockets))
            {
                debugLogHelper.LogError("SpecialSlotsHelper", $"Could not find pockets template with id {pocketsId}");
                continue;
            }

            if (pockets.Properties?.Slots == null)
            {
                debugLogHelper.LogError("SpecialSlotsHelper", $"Pockets template {pocketsId} has no slots.");
                continue;
            }

            foreach (var slot in pockets.Properties.Slots)
            {
                if (slot.Properties?.Filters == null)
                {
                    continue;
                }

                var firstFilter = slot.Properties.Filters.FirstOrDefault();
                if (firstFilter?.Filter == null)
                {
                    continue;
                }

                if (firstFilter.Filter.Add(request.NewId))
                {
                    debugLogHelper.LogService("SpecialSlotsHelper", $"Added {request.NewId} to pockets slot in {pocketsId}");
                }
            }
        }
    }
}