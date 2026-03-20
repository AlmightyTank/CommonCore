namespace CommonCore.Traders.Service.Sub;

using CommonCore.Traders.Models;
using SPTarkov.DI.Annotations;

[Injectable]
public sealed class TraderSanityService
{
    public void Apply(TraderLoadContext context)
    {
        if (string.IsNullOrWhiteSpace(context.TraderBase.Id))
        {
            context.TraderBase.Id = context.Definition.TraderId;
        }

        context.TraderBase.ItemsBuy ??= new()
        {
            Category = [],
            IdList = []
        };

        context.TraderBase.ItemsBuyProhibited ??= new()
        {
            Category = [],
            IdList = []
        };

        context.TraderBase.ItemsSell ??= [];
    }
}
