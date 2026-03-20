using CommonCore.Core;
using CommonCore.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonCore.Traders.Helper;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public sealed class TraderRegistrationHelper(
    CommonCoreDb db,
    CoreDebugLogHelper debugLogHelper)
{
    public void SetTraderUpdateTime(
        TraderConfig traderConfig,
        TraderBase traderBase,
        int refreshTimeSecondsMin,
        int refreshTimeSecondsMax)
    {
        traderConfig.UpdateTime.RemoveAll(x => x.TraderId == traderBase.Id);

        var traderRefreshRecord = new UpdateTime
        {
            TraderId = traderBase.Id,
            Seconds = new MinMax<int>(refreshTimeSecondsMin, refreshTimeSecondsMax)
        };

        traderConfig.UpdateTime.Add(traderRefreshRecord);
    }

    public void AddTraderToDb(TraderBase traderBase, TraderAssort assort)
    {
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

        if (!db.Traders.TryAdd(traderBase.Id, traderData))
        {
            debugLogHelper.LogService("Registration", $"Trader already exists in DB: {traderBase.Id}");
        }
    }

    public void AddTraderToLocales(
        TraderBase traderBase,
        string firstName,
        string description)
    {
        var locales = db.Locales.Global;
        var traderId = traderBase.Id;
        var fullName = traderBase.Name;
        var nickName = traderBase.Nickname;
        var location = traderBase.Location;

        foreach (var (_, locale) in locales)
        {
            locale.AddTransformer(lazyLoadedLocaleData =>
            {
                lazyLoadedLocaleData.TryAdd($"{traderId} FullName", fullName);
                lazyLoadedLocaleData.TryAdd($"{traderId} FirstName", firstName);
                lazyLoadedLocaleData.TryAdd($"{traderId} Nickname", nickName);
                lazyLoadedLocaleData.TryAdd($"{traderId} Location", location);
                lazyLoadedLocaleData.TryAdd($"{traderId} Description", description);
                return lazyLoadedLocaleData;
            });
        }
    }

    public void OverwriteTraderAssort(string traderId, TraderAssort newAssort)
    {
        if (!db.Traders.TryGetValue(traderId, out var trader))
        {
            debugLogHelper.LogError("Registration", $"Unable to update assort for trader: {traderId}");
            return;
        }

        trader.Assort = newAssort;
    }
}