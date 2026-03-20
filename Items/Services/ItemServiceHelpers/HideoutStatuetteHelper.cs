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
public sealed class HideoutStatuetteHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db)
{
    private const string CustomizationItem = "673c7b00cbf4b984b5099181";

    private static readonly string[] StatuetteSlotIds =
    [
        "Statuette_Gym_1",
        "Statuette_PlaceOfFame_1",
        "Statuette_PlaceOfFame_2",
        "Statuette_PlaceOfFame_3",
        "Statuette_Heating_1",
        "Statuette_Heating_2",
        "Statuette_Library_1",
        "Statuette_Library_2",
        "Statuette_RestSpace_1",
        "Statuette_RestSpace_2",
        "Statuette_MedStation_1",
        "Statuette_MedStation_2",
        "Statuette_Kitchen_1",
        "Statuette_Kitchen_2",
        "Statuette_BoozeGenerator_1",
        "Statuette_Workbench_1",
        "Statuette_IntelligenceCenter_1",
        "Statuette_ShootingRange_1"
    ];

    public void Process(ItemCreationRequest request)
    {
        if (!db.Items.TryGetValue(CustomizationItem, out var statuetteParent) ||
            statuetteParent.Properties?.Slots == null)
        {
            debugLogHelper.LogError("HideoutStatuetteHelper", $"Customization item {CustomizationItem} not found or has no slots.");
            return;
        }

        foreach (var statuetteSlotId in StatuetteSlotIds)
        {
            AddItemToStatuetteSlots(request.NewId, statuetteParent, statuetteSlotId);
        }
    }

    private void AddItemToStatuetteSlots(string itemId, TemplateItem statuetteItem, string statuetteSlotId)
    {
        if (statuetteItem.Properties?.Slots == null)
        {
            return;
        }

        foreach (var slot in statuetteItem.Properties.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Name) || slot.Properties?.Filters == null)
            {
                continue;
            }

            var slotType = GetMatchingSlotType(slot.Name);
            if (!string.Equals(statuetteSlotId, slotType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AddItemToFilters(itemId, slot, statuetteItem.Name);
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
                debugLogHelper.LogError("HideoutStatuetteHelper", $"Added {itemId} to slot '{slot.Name}' in {slotName}");
            }
            else
            {
                debugLogHelper.LogError("HideoutStatuetteHelper", $"{itemId} already in slot '{slot.Name}'");
            }
        }
    }

    private static string? GetMatchingSlotType(string slotName)
    {
        foreach (var type in StatuetteSlotIds)
        {
            if (slotName.StartsWith(type, StringComparison.OrdinalIgnoreCase))
            {
                return type;
            }
        }

        return null;
    }
}