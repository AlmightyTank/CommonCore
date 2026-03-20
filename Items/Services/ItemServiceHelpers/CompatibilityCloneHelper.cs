using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using CommonCore.Items.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public class CompatibilityCloneHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db,
    CompatibilityService compatibilityService
)
{
    public void Process(ItemCreationRequest request)
    {
        if (!request.AmmoCloneCompatibility &&
            !request.WeaponCloneChamberCompatibility &&
            !request.MagCloneCartridgeCompatibility)
        {
            return;
        }

        if (db?.Items == null)
        {
            debugLogHelper.LogError("CompatibilityCloneHelper", $"Templates missing");
            return;
        }

        if (!db.Items.TryGetValue(request.NewId, out var newItem))
        {
            debugLogHelper.LogError("CompatibilityCloneHelper", $"New item '{request.NewId}' not found");
            return;
        }

        var cloneId = request.ItemTplToClone;
        if (string.IsNullOrWhiteSpace(cloneId))
        {
            debugLogHelper.LogError("CompatibilityCloneHelper", $"ItemTplToClone missing for {request.NewId}");
            return;
        }

        if (!db.Items.TryGetValue(cloneId, out var sourceItem))
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
                debugLogHelper.LogError("CompatibilityCloneHelper", $"Properties missing for {request.NewId}");
                return;
            }

            var newMongoId = new MongoId(request.NewId);
            var cloneMongoId = new MongoId(cloneId);

            if (request.AmmoCloneCompatibility)
            {
                targetProps.AmmoCaliber = sourceProps.AmmoCaliber;
                compatibilityService.AddAmmoClone(newMongoId, cloneMongoId);
                debugLogHelper.LogService("CompatibilityCloneHelper", $"Cloned ammo caliber from {cloneId} -> {request.NewId}");
            }

            if (request.WeaponCloneChamberCompatibility)
            {
                var chamberCloneId = request.WeaponCloneChamberId ?? cloneId;

                if (db.Items.TryGetValue(chamberCloneId, out var chamberSource)
                    && chamberSource.Properties?.Chambers != null)
                {
                    targetProps.Chambers = chamberSource.Properties.Chambers
                        .Select(CloneSlot)
                        .ToList();

                    compatibilityService.AddAmmoClone(newMongoId, new MongoId(chamberCloneId));
                    debugLogHelper.LogService("CompatibilityCloneHelper", $"Cloned Chambers from {chamberCloneId} -> {request.NewId}");
                }
                else
                {
                    debugLogHelper.LogError("CompatibilityCloneHelper", $"Chamber source '{chamberCloneId}' invalid for {request.NewId}");
                }
            }

            if (request.MagCloneCartridgeCompatibility)
            {
                var magCloneId = request.MagCloneCartridgeId ?? cloneId;

                if (db.Items.TryGetValue(magCloneId, out var magSource)
                    && magSource.Properties?.Cartridges != null)
                {
                    targetProps.Cartridges = magSource.Properties.Cartridges
                        .Select(CloneSlot)
                        .ToList();

                    compatibilityService.AddAmmoClone(newMongoId, new MongoId(magCloneId));
                    debugLogHelper.LogService("CompatibilityCloneHelper", $"Cloned Cartridges from {magCloneId} -> {request.NewId}");
                }
                else
                {
                    debugLogHelper.LogError("CompatibilityCloneHelper", $"Magazine source '{magCloneId}' invalid for {request.NewId}");
                }
            }
        }
        catch (Exception ex)
        {
            debugLogHelper.LogError("CompatibilityCloneHelper", $"Failed for {request.NewId}: {ex.Message}");
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