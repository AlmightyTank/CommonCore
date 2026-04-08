using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonLibExtended.Items.Services.ItemHelpers;

[Injectable]
public class EquipmentSlotHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService)
{
    private static readonly MongoId PlayerInventoryId = new("55d7217a4bdc2d86028b456d");

    public void Process(ItemModificationRequest request)
    {
        if (!request.Extras.AddToPrimaryWeaponSlot == true && !request.Extras.AddToHolsterWeaponSlot == true)
        {
            return;
        }

        if (!databaseService.GetItems().TryGetValue(PlayerInventoryId, out var playerInventory))
        {
            debugLogHelper.LogError("EquipmentSlotHelper", $"Player inventory template not found.");
            return;
        }

        if (request.Extras.AddToPrimaryWeaponSlot == true)
        {
            AddToSlot(request.ItemId, playerInventory, "FirstPrimaryWeapon");
            AddToSlot(request.ItemId, playerInventory, "SecondPrimaryWeapon");
        }

        if (request.Extras.AddToHolsterWeaponSlot == true)
        {
            AddToSlot(request.ItemId, playerInventory, "Holster");
        }
    }

    private void AddToSlot(string itemId, TemplateItem inventoryItem, string slotName)
    {
        var slots = inventoryItem.Properties?.Slots?.ToList();
        if (slots == null || slots.Count == 0)
        {
            debugLogHelper.LogError("EquipmentSlotHelper", $"Inventory template has no slots. Could not add {itemId} to '{slotName}'.");
            return;
        }

        var slot = slots.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.Name) &&
            x.Name.Equals(slotName, StringComparison.OrdinalIgnoreCase));

        if (slot == null)
        {
            debugLogHelper.LogError("EquipmentSlotHelper", $"Slot '{slotName}' not found on player inventory.");
            return;
        }

        var filters = slot.Properties?.Filters?.ToList();
        if (filters == null || filters.Count == 0)
        {
            debugLogHelper.LogError("EquipmentSlotHelper", $"Slot '{slotName}' has no filters.");
            return;
        }

        var firstFilter = filters.FirstOrDefault();
        if (firstFilter == null)
        {
            debugLogHelper.LogError("EquipmentSlotHelper", $"Slot '{slotName}' first filter missing.");
            return;
        }

        firstFilter.Filter ??= new HashSet<MongoId>();

        if (firstFilter.Filter.Add(itemId))
        {
            debugLogHelper.LogService("EquipmentSlot",$"Added {itemId} to '{slotName}'");
        }
        else
        {
            debugLogHelper.LogService("EquipmentSlot", $"{itemId} already exists in '{slotName}'");
        }
    }
}