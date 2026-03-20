using CommonCore.Helpers;
using CommonCore.Items.Models;
using CommonCore.Items.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public class CraftHelper(
    CoreDebugLogHelper debugLogHelper,
    ContentService contentService
)
{
    public void Process(ItemCreationRequest request)
    {
        if (!request.AddCrafts)
        {
            return;
        }

        if (request.Crafts == null || request.Crafts.Length == 0)
        {
            debugLogHelper.LogError("CraftHelper", $"Invalid crafts for {request.NewId}");
            return;
        }

        contentService.AddCrafts(request.Crafts);
        debugLogHelper.LogService("CraftHelper", $"Added {request.Crafts.Length} crafts for {request.NewId}");
    }
}