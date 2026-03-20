using CommonCore.Core;
using CommonCore.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonCore.Loading;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader)]
public sealed class CommonCoreLoad(
    Core.CommonCore core) : IOnLoad
{
    public Task OnLoad()
    {
        LogHelper.Log("CommonCore loaded");
        core.OnLoad();
        return Task.CompletedTask;
    }
}