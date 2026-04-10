using CommonLibExtended.Helpers;
using CommonLibExtended.Traders.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Services;

namespace CommonLibExtended.Traders.Services;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 100)]
public sealed class CustomTraderPricingPostProcessor(
    DatabaseService db,
    CustomTraderPricingRegistry registry,
    CustomTraderSettingsHelper helper,
    DebugLogHelper log) : IOnLoad
{
    private readonly bool isEnabled = true;
    public Task OnLoad()
    {
        if (!isEnabled)
            return Task.CompletedTask;

        var traders = db.GetTables().Traders;

        foreach (var (id, settings) in registry.GetAll())
        {
            if (!traders.TryGetValue(id, out var trader) || trader?.Assort == null)
                continue;

            helper.ApplyPricing(trader.Assort, settings);

            log.LogService(nameof(CustomTraderPricingPostProcessor),
                $"Repriced trader {id}",
                nameof(OnLoad));
        }

        return Task.CompletedTask;
    }
}