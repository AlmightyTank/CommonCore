using CommonCore.Core;
using CommonCore.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonCore.Loading;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 10)]
public sealed class CommonCorePostLoad(
    CommonCore.Core.CommonCore core) : IOnLoad
{
    public Task OnLoad()
    {
        LogHelper.Log("CommonCore post-load");
        core.PostLoad();
        return Task.CompletedTask;
    }
}