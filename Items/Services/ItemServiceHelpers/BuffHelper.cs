using CommonCore.Helpers;
using CommonCore.Items.Models;
using CommonCore.Items.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public class BuffHelper(
    CoreDebugLogHelper debugLogHelper,
    ContentService contentService
)
{
    public void Process(ItemCreationRequest request)
    {
        if (!request.AddBuffs)
        {
            return;
        }

        if (request.Buffs == null || request.Buffs.Count == 0)
        {
            debugLogHelper.LogError("BuffHelper", $"Invalid buffs for {request.NewId}");
            return;
        }

        contentService.AddBuffs(request.Buffs);
        debugLogHelper.LogService("BuffHelper", $"Added buffs for {request.NewId}");
    }
}