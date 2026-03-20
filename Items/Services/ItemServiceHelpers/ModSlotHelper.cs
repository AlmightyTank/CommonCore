using CommonCore.Helpers;
using CommonCore.Items.Models;
using CommonCore.Items.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public class ModSlotHelper(
    CoreDebugLogHelper debugLogHelper,
    CompatibilityService compatibilityService
)
{
    public void Process(ItemCreationRequest request)
    {
        if (!request.AddToModSlots)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.NewId))
        {
            debugLogHelper.LogError("ModSlotHelper", $"NewId is null or empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(request.ItemTplToClone))
        {
            debugLogHelper.LogError("ModSlotHelper", $"ItemTplToClone missing for {request.NewId}");
            return;
        }

        var modSlots = request.ModSlot;
        if (modSlots == null || modSlots.Length == 0)
        {
            debugLogHelper.LogError("ModSlotHelper", $"ModSlot missing for {request.NewId}");
            return;
        }

        try
        {
            var newItemId = new MongoId(request.NewId);
            var cloneId = new MongoId(request.AddtoModSlotsCloneId ?? request.ItemTplToClone);

            compatibilityService.AddToModSlots(
                newItemId,
                cloneId,
                modSlots,
                request.AddtoConflicts);

            debugLogHelper.LogService("ModSlotHelper", $"Queued {request.NewId} for compatibility processing.");
        }
        catch (Exception ex)
        {
            debugLogHelper.LogError("ModSlotHelper", $"Failed for {request.NewId}: {ex.Message}");
        }
    }
}