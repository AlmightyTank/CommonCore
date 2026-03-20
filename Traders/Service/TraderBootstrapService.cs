namespace CommonCore.Traders.Service;

using CommonCore.Core;
using CommonCore.Traders.Models;
using CommonCore.Traders.Service.Sub;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Reflection;
using CommonCore.Helpers;
using CommonCore.Traders.Services;

[Injectable]
public sealed class TraderBootstrapService(
    ModHelper modHelper,
    TraderConfigService traderConfigService,
    TraderSanityService traderSanityService,
    TraderSettingsApplyService traderSettingsApplyService,
    TraderUnlockCoordinator traderUnlockCoordinator,
    TraderStockService traderStockService,
    TraderPricingService traderPricingService,
    TraderImageService traderImageService,
    TraderRefreshService traderRefreshService,
    TraderRegistrationService traderRegistrationService,
    TraderLocaleService traderLocaleService,
    CoreDebugLogHelper traderDebugLogService)
{
    public void LoadTrader(Assembly assembly, ITraderDefinition definition)
    {
        var modPath = modHelper.GetAbsolutePathToModFolder(assembly);

        var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(
            modPath,
            definition.BaseFilePath);

        var assort = modHelper.GetJsonDataFromFile<TraderAssort>(
            modPath,
            definition.AssortFilePath);

        var settings = traderConfigService.LoadOrCreate(
            modPath,
            definition);

        var context = new TraderLoadContext
        {
            ModPath = modPath,
            Definition = definition,
            TraderBase = traderBase,
            Assort = assort,
            Settings = settings
        };

        traderDebugLogService.Initialize(modPath, settings.DebugLogging);

        traderDebugLogService.Log($"Loading trader: {definition.TraderId}");
        traderDebugLogService.LogConfig(settings);

        traderSanityService.Apply(context);
        definition.Configure(context);
        traderSettingsApplyService.Apply(context);
        traderUnlockCoordinator.Apply(context);
        traderStockService.Apply(context);
        traderPricingService.Apply(context);
        traderImageService.Apply(context);
        traderRefreshService.Apply(context);
        traderRegistrationService.Apply(context);
        traderLocaleService.Apply(context);
    }
}