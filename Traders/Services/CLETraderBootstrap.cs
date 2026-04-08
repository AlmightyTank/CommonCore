using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using CommonLibExtended.Traders.Helpers;
using CommonLibExtended.Traders.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using System.Reflection;

namespace CommonLibExtended.Services;

[Injectable(InjectionType.Singleton)]
public sealed class CLETraderBootstrap(
    ISptLogger<CLETraderBootstrap> logger,
    ModPathHelper modPathHelper,
    CustomTraderHelper customTraderHelper,
    CustomTraderSettingsHelper customTraderSettingsHelper,
    CustomTraderUnlockService customTraderUnlockService)
{
    private readonly ISptLogger<CLETraderBootstrap> _logger = logger;
    private readonly ModPathHelper _modPathHelper = modPathHelper;
    private readonly CustomTraderHelper _customTraderHelper = customTraderHelper;
    private readonly CustomTraderSettingsHelper _customTraderSettingsHelper = customTraderSettingsHelper;
    private readonly CustomTraderUnlockService _customTraderUnlockService = customTraderUnlockService;

    public void LoadTrader(
        Assembly assembly,
        string traderBaseRelativePath,
        string assortRelativePath,
        string settingsRelativePath,
        TraderConfig traderConfig,
        RagfairConfig ragfairConfig,
        Func<string, TraderBase> loadTraderBase,
        Func<string, TraderAssort> loadTraderAssort,
        Func<string, CustomTraderSettings> loadTraderSettings,
        string? firstName = null,
        string description = "")
    {
        var traderBasePath = _modPathHelper.GetFullPath(assembly, traderBaseRelativePath);
        var assortPath = _modPathHelper.GetFullPath(assembly, assortRelativePath);
        var settingsPath = _modPathHelper.GetFullPath(assembly, settingsRelativePath);

        var traderBase = loadTraderBase(traderBasePath);
        var assort = loadTraderAssort(assortPath);
        var settings = loadTraderSettings(settingsPath);

        if (traderBase == null)
        {
            _logger.Error($"Failed to load trader base from {traderBasePath}");
            return;
        }

        if (assort == null)
        {
            _logger.Error($"Failed to load trader assort from {assortPath}");
            return;
        }

        if (settings == null)
        {
            _logger.Error($"Failed to load trader settings from {settingsPath}");
            return;
        }

        settings.Validate(traderBase.Id);

        _customTraderSettingsHelper.ApplyBaseSettings(traderBase, settings);
        _customTraderSettingsHelper.ApplyAssortSettings(assort, settings);
        _customTraderSettingsHelper.ApplyFleaSettings(ragfairConfig, traderBase, settings);
        _customTraderSettingsHelper.ApplyRefreshSettings(traderConfig, traderBase, settings);

        _customTraderHelper.AddTraderToDb(traderBase, assort);
        _customTraderHelper.AddTraderToLocales(
            traderBase,
            firstName ?? traderBase.Nickname ?? traderBase.Name ?? "Trader",
            description);

        _customTraderUnlockService.RegisterTrader(
            traderBase.Id,
            settings.MinLevel,
            settings.UnlockedByDefault);

        _logger.Info($"Loaded custom trader {traderBase.Id}");
    }
}