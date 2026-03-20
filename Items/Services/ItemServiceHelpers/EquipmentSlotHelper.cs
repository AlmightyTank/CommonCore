using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public class EquipmentSlotHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db)
{
    private static readonly MongoId PlayerInventoryId = new("55d7217a4bdc2d86028b456d");

    public void Process(ItemCreationRequest request)
    {
        if (!request.AddToPrimaryWeaponSlot && !request.AddToHolsterWeaponSlot)
        {
            return;
        }

        if (!db.Items.TryGetValue(PlayerInventoryId, out var playerInventory))
        {
            debugLogHelper.LogError("EquipmentSlotHelper", $"Player inventory template not found.");
            return;
        }

        if (request.AddToPrimaryWeaponSlot)
        {
            AddToSlot(request.NewId, playerInventory, "FirstPrimaryWeapon");
            AddToSlot(request.NewId, playerInventory, "SecondPrimaryWeapon");
        }

        if (request.AddToHolsterWeaponSlot)
        {
            AddToSlot(request.NewId, playerInventory, "Holster");
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
            LogHelper.LogDebug($"[EquipmentSlot] Added {itemId} to '{slotName}'");
        }
        else
        {
            LogHelper.LogDebug($"[EquipmentSlot] {itemId} already exists in '{slotName}'");
        }
    }
}