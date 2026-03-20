namespace CommonCore.Traders.Service;

using CommonCore.Helpers;
using CommonCore.Traders.Models;
using CommonCore.Traders.Services;
using SPTarkov.DI.Annotations;

[Injectable]
public sealed class TraderUnlockCoordinator(TraderProfileMonitorService traderUnlockService, CoreDebugLogHelper debugLogService)
{
    public void Apply(TraderLoadContext context)
    {
        debugLogService.LogService("Unlock", $"Trader={context.TraderBase.Id}, MinLevel={context.Settings.MinLevel}");
        if (!context.Settings.UnlockedByDefault)
        {
            debugLogService.LogService("Unlock", "Using level lock mode");
            TraderProfileMonitorService.EnableLevelLock = true;
            TraderProfileMonitorService.MinLevelRequired = context.Settings.MinLevel;
            TraderProfileMonitorService.ForceUnlock = false;
            traderUnlockService.OnLoad();
            return;
        }

        debugLogService.LogService("Unlock", "Using force unlock mode");
        TraderProfileMonitorService.EnableLevelLock = false;
        TraderProfileMonitorService.ForceUnlock = true;
    }
}