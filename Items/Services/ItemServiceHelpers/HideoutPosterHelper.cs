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
public sealed class HideoutPosterHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db)
{
    private const string CustomizationItem = "673c7b00cbf4b984b5099181";

    private static readonly string[] PosterSlotIds =
    [
        "Poster_Security_1",
        "Poster_Security_2",
        "Poster_Generator_1",
        "Poster_Generator_2",
        "Poster_ScavCase_1",
        "Poster_ScavCase_2",
        "Poster_Stash_1",
        "Poster_WaterCloset_1",
        "Poster_ShootingRange_1",
        "Poster_Workbench_1",
        "Poster_IntelligenceCenter_1",
        "Poster_Kitchen_1",
        "Poster_MedStation_1",
        "Poster_AirFilteringUnit_1",
        "Poster_RestSpace_1",
        "Poster_RestSpace_2",
        "Poster_RestSpace_3",
        "Poster_RestSpace_4",
        "Poster_Heating_1",
        "Poster_Heating_2",
        "Poster_Heating_3",
        "Poster_Gym_1",
        "Poster_Gym_2",
        "Poster_Gym_3",
        "Poster_Gym_4",
        "Poster_Gym_5",
        "Poster_Gym_6",
        "Poster_Security_3",
        "Poster_ShootingRange_2"
    ];

    public void Process(ItemCreationRequest request)
    {
        if (!db.Items.TryGetValue(CustomizationItem, out var posterParent) || posterParent.Properties?.Slots == null)
        {
            debugLogHelper.LogError("HideoutPosterHelper", $"Customization item {CustomizationItem} not found or has no slots.");
            return;
        }

        foreach (var posterSlotId in PosterSlotIds)
        {
            AddItemToPosterSlots(request.NewId, posterParent, posterSlotId);
        }
    }

    private void AddItemToPosterSlots(string itemId, TemplateItem posterItem, string posterSlotId)
    {
        if (posterItem.Properties?.Slots == null)
        {
            return;
        }

        foreach (var slot in posterItem.Properties.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Name) || slot.Properties?.Filters == null)
            {
                continue;
            }

            var slotType = GetMatchingSlotType(slot.Name);
            if (!string.Equals(posterSlotId, slotType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AddItemToFilters(itemId, slot, posterItem.Name);
        }
    }

    private void AddItemToFilters(string itemId, Slot slot, string? slotName)
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
                debugLogHelper.LogService("HideoutPosterHelper", $"Added {itemId} to slot '{slot.Name}' in {slotName}");
            }
            debugLogHelper.LogService("HideoutPosterHelper", $"{itemId} already in slot '{slot.Name}'");
        }
    }

    private static string? GetMatchingSlotType(string slotName)
    {
        foreach (var type in PosterSlotIds)
        {
            if (slotName.StartsWith(type, StringComparison.OrdinalIgnoreCase))
            {
                return type;
            }
        }

        return null;
    }
}