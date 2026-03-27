using CommonCore.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace CommonCore.Helpers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public class CompatibilityCloneHelper(
    CoreDebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    CompatibilityService compatibilityService
)
{
    public void Process(CommonCoreItemRequest request)
    {
        if (!(request.Config.AmmoCloneCompatibility ?? false) &&
            !(request.Config.WeaponCloneChamberCompatibility ?? false) &&
            !(request.Config.MagCloneCartridgeCompatibility ?? false))
        {
            return;
        }

        if (databaseService?.GetItems() == null)
        {
            debugLogHelper.LogError("CompatibilityCloneHelper", $"Templates missing");
            return;
        }

        if (!databaseService.GetItems().TryGetValue(request.ItemId, out var newItem))
        {
            debugLogHelper.LogError("CompatibilityCloneHelper", $"New item '{request.ItemId}' not found");
            return;
        }

        var cloneId = request.Config.ItemTplToClone;
        if (string.IsNullOrWhiteSpace(cloneId))
        {
            debugLogHelper.LogError("CompatibilityCloneHelper", $"ItemTplToClone missing for {request.ItemId}");
            return;
        }

        if (!databaseService.GetItems().TryGetValue(cloneId, out var sourceItem))
        {
            debugLogHelper.LogError("CompatibilityCloneHelper", $"Source item '{cloneId}' not found");
            return;
        }

        try
        {
            var sourceProps = sourceItem.Properties;
            var targetProps = newItem.Properties;

            if (sourceProps == null || targetProps == null)
            {
                debugLogHelper.LogError("CompatibilityCloneHelper", $"Properties missing for {request.ItemId}");
                return;
            }

            var newMongoId = new MongoId(request.ItemId);
            var cloneMongoId = new MongoId(cloneId);

            if (request.Config.AmmoCloneCompatibility == true)
            {
                targetProps.AmmoCaliber = sourceProps.AmmoCaliber;
                compatibilityService.AddAmmoClone(newMongoId, cloneMongoId);
                debugLogHelper.LogService("CompatibilityCloneHelper", $"Cloned ammo caliber from {cloneId} -> {request.ItemId}");
            }

            if (request.Config.WeaponCloneChamberCompatibility == true)
            {
                var chamberCloneId = request.Config.WeaponCloneChamberId ?? cloneId;

                if (databaseService.GetItems().TryGetValue(chamberCloneId, out var chamberSource)
                    && chamberSource.Properties?.Chambers != null)
                {
                    targetProps.Chambers = chamberSource.Properties.Chambers
                        .Select(CloneSlot)
                        .ToList();

                    compatibilityService.AddAmmoClone(newMongoId, new MongoId(chamberCloneId));
                    debugLogHelper.LogService("CompatibilityCloneHelper", $"Cloned Chambers from {chamberCloneId} -> {request.ItemId}");
                }
                else
                {
                    debugLogHelper.LogError("CompatibilityCloneHelper", $"Chamber source '{chamberCloneId}' invalid for {request.ItemId}");
                }
            }

            if (request.Config.MagCloneCartridgeCompatibility == true)
            {
                var magCloneId = request.Config.MagCloneCartridgeId ?? cloneId;

                if (databaseService.GetItems().TryGetValue(magCloneId, out var magSource)
                    && magSource.Properties?.Cartridges != null)
                {
                    targetProps.Cartridges = magSource.Properties.Cartridges
                        .Select(CloneSlot)
                        .ToList();

                    compatibilityService.AddAmmoClone(newMongoId, new MongoId(magCloneId));
                    debugLogHelper.LogService("CompatibilityCloneHelper", $"Cloned Cartridges from {magCloneId} -> {request.ItemId}");
                }
                else
                {
                    debugLogHelper.LogError("CompatibilityCloneHelper", $"Magazine source '{magCloneId}' invalid for {request.ItemId}");
                }
            }
        }
        catch (Exception ex)
        {
            debugLogHelper.LogError("CompatibilityCloneHelper", $"Failed for {request.ItemId}: {ex.Message}");
        }
    }

    private static Slot CloneSlot(Slot slot)
    {
        List<SlotFilter>? clonedFilters = null;

        if (slot.Properties?.Filters != null)
        {
            clonedFilters = slot.Properties.Filters
                .Select(filter => new SlotFilter
                {
                    Filter = filter.Filter != null
                        ? new HashSet<MongoId>(filter.Filter)
                        : null
                })
                .ToList();
        }

        return new Slot
        {
            Name = slot.Name,
            Id = slot.Id,
            Parent = slot.Parent,
            Required = slot.Required,
            MergeSlotWithChildren = slot.MergeSlotWithChildren,
            Prototype = slot.Prototype,
            Properties = new SlotProperties
            {
                Filters = clonedFilters
            }
        };
    }
}