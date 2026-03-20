namespace CommonCore.Traders.Service.Sub;

using CommonCore.Helpers;
using CommonCore.Traders.Helper;
using CommonCore.Traders.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;

[Injectable]
public sealed class TraderLocaleService(TraderRegistrationHelper traderRegistrationHelper, CoreDebugLogHelper debugLogService)
{
    public void Apply(TraderLoadContext context)
    {
        debugLogService.LogService("Locale",
            $"Locale entries added for {context.TraderBase.Id}");
        var localeName = context.TraderBase.Nickname
            ?? context.TraderBase.Name
            ?? context.Definition.DefaultLocaleName;

        traderRegistrationHelper.AddTraderToLocales(
            context.TraderBase,
            localeName,
            context.Definition.DefaultLocaleDescription);
    }
}