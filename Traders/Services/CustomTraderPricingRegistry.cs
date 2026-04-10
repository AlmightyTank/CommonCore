using CommonLibExtended.Traders.Models;
using SPTarkov.DI.Annotations;

namespace CommonLibExtended.Traders.Services;

[Injectable]
public sealed class CustomTraderPricingRegistry
{
    private readonly Dictionary<string, CustomTraderSettings> _map = new();

    public void Register(string traderId, CustomTraderSettings settings)
        => _map[traderId] = settings;

    public IReadOnlyDictionary<string, CustomTraderSettings> GetAll()
        => _map;
}