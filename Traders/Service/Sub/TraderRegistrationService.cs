namespace CommonCore.Traders.Service.Sub;

using CommonCore.Helpers;
using CommonCore.Traders.Helper;
using CommonCore.Traders.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;

[Injectable]
public sealed class TraderRegistrationService(
    ConfigServer configServer,
    TraderRegistrationHelper traderRegistrationHelper,
    CoreDebugLogHelper debugLogService
    )
{
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();

    public void Apply(TraderLoadContext context)
    {
        if (context.Settings.AddTraderToFleaMarket)
        {
            debugLogService.Log("Registering trader into DB");
            debugLogService.LogService("Registration",
                $"FleaEnabled={context.Settings.AddTraderToFleaMarket}");
            _ragfairConfig.Traders.TryAdd(context.TraderBase.Id, true);
        }
        else
        {
            _ragfairConfig.Traders.Remove(context.TraderBase.Id);
        }

        traderRegistrationHelper.AddTraderToDb(context.TraderBase, context.Assort);
    }
}
