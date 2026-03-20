namespace CommonCore.Traders.Service.Sub;

using CommonCore.Helpers;
using CommonCore.Traders.Helper;
using CommonCore.Traders.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;

[Injectable]
public sealed class TraderRefreshService(
    ConfigServer configServer,
    TraderRegistrationHelper traderRegistrationHelper,
    CoreDebugLogHelper debugLogService)
{
    private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();

    public void Apply(TraderLoadContext context)
    {
        var minRefresh = Math.Min(context.Settings.TraderRefreshMin, context.Settings.TraderRefreshMax);
        var maxRefresh = Math.Max(context.Settings.TraderRefreshMin, context.Settings.TraderRefreshMax);
        var restockTime = new Random().Next(minRefresh, maxRefresh + 1);

        debugLogService.LogService("Refresh",
            $"Restock time set to {restockTime}s");

        traderRegistrationHelper.SetTraderUpdateTime(
            _traderConfig,
            context.TraderBase,
            restockTime,
            restockTime);

        context.TraderBase.NextResupply = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + restockTime);
    }
}
