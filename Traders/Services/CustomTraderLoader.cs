using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using CommonLibExtended.Traders.Models;
using CommonLibExtended.Traders.TraderHelpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace CommonLibExtended.Services;

[Injectable]
public sealed class CustomTraderLoader(
    DebugLogHelper debugLogHelper,
    CustomTraderHelper customTraderHelper,
    CustomTraderSettingsHelper customTraderSettingsHelper)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly CustomTraderHelper _customTraderHelper = customTraderHelper;
    private readonly CustomTraderSettingsHelper _customTraderSettingsHelper = customTraderSettingsHelper;

    public void LoadTrader(
        TraderBase traderBase,
        TraderAssort assort,
        CustomTraderSettings settings,
        string firstName,
        string description)
    {
        if (traderBase == null)
        {
            _debugLogHelper.LogError("CustomTraderLoader", "Trader base was null");
            return;
        }

        if (assort == null)
        {
            _debugLogHelper.LogError("CustomTraderLoader", $"Trader assort was null for {traderBase.Id}");
            return;
        }

        if (settings == null)
        {
            _debugLogHelper.LogError("CustomTraderLoader", $"Trader settings were null for {traderBase.Id}");
            return;
        }

        settings.Validate(traderBase.Id);

        _customTraderSettingsHelper.ApplyBaseSettings(traderBase, settings);
        _customTraderSettingsHelper.ApplyAssortSettings(assort, settings);
        _customTraderSettingsHelper.ApplyFleaSettings(traderBase, settings);
        _customTraderSettingsHelper.ApplyRefreshSettings(traderBase, settings);

        _customTraderHelper.AddTraderToDb(traderBase, assort);
        _customTraderHelper.AddTraderToLocales(traderBase, firstName, description);

        _debugLogHelper.LogService("CustomTraderLoader", $"Loaded trader {traderBase.Id}");
    }
}