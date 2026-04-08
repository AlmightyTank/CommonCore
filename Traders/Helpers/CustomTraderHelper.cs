using CommonLibExtended.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Services;

namespace CommonLibExtended.Traders.Helpers;

[Injectable]
public sealed class CustomTraderHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly DatabaseService _databaseService = databaseService;

    public void SetTraderUpdateTime(
        TraderConfig traderConfig,
        TraderBase traderBase,
        int refreshTimeSecondsMin,
        int refreshTimeSecondsMax)
    {
        if (traderConfig == null || traderBase == null)
        {
            return;
        }

        traderConfig.UpdateTime.RemoveAll(x => x.TraderId == traderBase.Id);

        traderConfig.UpdateTime.Add(new UpdateTime
        {
            TraderId = traderBase.Id,
            Seconds = new MinMax<int>(refreshTimeSecondsMin, refreshTimeSecondsMax)
        });

        _debugLogHelper.LogService(
            "CustomTraderHelper",
            $"Set update time for trader {traderBase.Id} to {refreshTimeSecondsMin}-{refreshTimeSecondsMax} seconds");
    }

    public void AddTraderToDb(TraderBase traderBase, TraderAssort assort)
    {
        if (traderBase == null || assort == null)
        {
            return;
        }

        var traderData = new Trader
        {
            Assort = assort,
            Base = traderBase,
            QuestAssort = new()
            {
                { "Started", new() },
                { "Success", new() },
                { "Fail", new() }
            },
            Dialogue = []
        };

        if (!_databaseService.GetTables().Traders.TryAdd(traderBase.Id, traderData))
        {
            _debugLogHelper.LogError("CustomTraderHelper", $"Trader already exists in DB: {traderBase.Id}");
            return;
        }

        _debugLogHelper.LogService("CustomTraderHelper", $"Added trader {traderBase.Id} to database");
    }

    public void AddTraderToLocales(
        TraderBase traderBase,
        string firstName,
        string description)
    {
        if (traderBase == null)
        {
            return;
        }

        var locales = _databaseService.GetTables().Locales.Global;
        var traderId = traderBase.Id;
        var fullName = traderBase.Name;
        var nickName = traderBase.Nickname;
        var location = traderBase.Location;

        foreach (var (_, localeKvp) in locales)
        {
            localeKvp.AddTransformer(localeData =>
            {
                localeData.TryAdd($"{traderId} FullName", fullName);
                localeData.TryAdd($"{traderId} FirstName", firstName);
                localeData.TryAdd($"{traderId} Nickname", nickName);
                localeData.TryAdd($"{traderId} Location", location);
                localeData.TryAdd($"{traderId} Description", description);
                return localeData;
            });
        }

        _debugLogHelper.LogService("CustomTraderHelper", $"Added locale entries for trader {traderId}");
    }

    public void OverwriteTraderAssort(string traderId, TraderAssort newAssort)
    {
        if (string.IsNullOrWhiteSpace(traderId) || newAssort == null)
        {
            return;
        }

        if (!_databaseService.GetTables().Traders.TryGetValue(traderId, out var trader))
        {
            _debugLogHelper.LogError("CustomTraderHelper", $"Unable to update assort for trader: {traderId}");
            return;
        }

        trader.Assort = newAssort;
        _debugLogHelper.LogService("CustomTraderHelper", $"Overwrote assort for trader {traderId}");
    }
}