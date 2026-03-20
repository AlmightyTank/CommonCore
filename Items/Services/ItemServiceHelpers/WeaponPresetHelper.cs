using CommonCore.Helpers;
using CommonCore.Items.Models;
using CommonCore.Items.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public class WeaponPresetHelper(
    CoreDebugLogHelper debugLogHelper,
    ContentService contentService
)
{
    public void Process(ItemCreationRequest request)
    {
        if (!request.AddToPreset)
        {
            return;
        }

        if (request.Presets == null || request.Presets.Length == 0)
        {
            debugLogHelper.LogError("WeaponPresetHelper", $"Invalid presets for {request.NewId}");
            return;
        }

        foreach (var preset in request.Presets)
        {
            if (preset == null)
            {
                continue;
            }

            contentService.AddPreset(preset);
        }

        debugLogHelper.LogService("WeaponPresetHelper", $"Added {request.Presets.Length} presets for {request.NewId}");
    }
}