using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using CommonLibExtended.Services;
using CommonLibExtended.Traders.Helpers;
using CommonLibExtended.Traders.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Routers;
using System.Reflection;

namespace CommonLibExtended.Traders.Services;

[Injectable(InjectionType.Singleton)]
public sealed class CLETraderBootstrap(
    DebugLogHelper debugLogHelper,
    ModPathHelper modPathHelper,
    CustomTraderLoader customTraderLoader,
    CustomTraderUnlockService customTraderUnlockService)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly ModPathHelper _modPathHelper = modPathHelper;
    private readonly CustomTraderLoader _customTraderLoader = customTraderLoader;
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
        string description = "",
        ImageRouter? imageRouter = null,
        string? traderImageRelativePath = null,
        string? avatarRouteOverride = null)
    {
        var traderBasePath = _modPathHelper.GetFullPath(assembly, traderBaseRelativePath);
        var assortPath = _modPathHelper.GetFullPath(assembly, assortRelativePath);
        var settingsPath = _modPathHelper.GetFullPath(assembly, settingsRelativePath);

        var traderBase = loadTraderBase(traderBasePath);
        var assort = loadTraderAssort(assortPath);
        var settings = loadTraderSettings(settingsPath);

        if (traderBase == null)
        {
            _debugLogHelper.LogError(nameof(CLETraderBootstrap), $"Failed to load trader base from {traderBasePath}");
            return;
        }

        if (assort == null)
        {
            _debugLogHelper.LogError(nameof(CLETraderBootstrap), $"Failed to load trader assort from {assortPath}");
            return;
        }

        if (settings == null)
        {
            _debugLogHelper.LogError(nameof(CLETraderBootstrap), $"Failed to load trader settings from {settingsPath}");
            return;
        }

        settings.Validate(traderBase.Id);

        RegisterTraderImageIfProvided(
            assembly,
            traderBase,
            imageRouter,
            traderImageRelativePath,
            avatarRouteOverride);

        _customTraderLoader.LoadTrader(
            traderBase,
            assort,
            settings,
            traderConfig,
            ragfairConfig,
            firstName ?? traderBase.Nickname ?? traderBase.Name ?? "Trader",
            description);

        _customTraderUnlockService.RegisterTrader(
            traderBase.Id,
            settings.MinLevel,
            settings.UnlockedByDefault);

        _debugLogHelper.LogService(nameof(CLETraderBootstrap), $"Loaded custom trader {traderBase.Id}");
    }

    private void RegisterTraderImageIfProvided(
        Assembly assembly,
        TraderBase traderBase,
        ImageRouter? imageRouter,
        string? traderImageRelativePath,
        string? avatarRouteOverride)
    {
        if (imageRouter == null || string.IsNullOrWhiteSpace(traderImageRelativePath))
        {
            return;
        }

        var imagePath = _modPathHelper.GetFullPath(assembly, traderImageRelativePath);
        if (!File.Exists(imagePath))
        {
            _debugLogHelper.LogError(nameof(CLETraderBootstrap), $"Trader image file not found: {imagePath}");
            return;
        }

        var avatarRoute = !string.IsNullOrWhiteSpace(avatarRouteOverride)
            ? avatarRouteOverride
            : NormalizeAvatarRoute(traderBase.Avatar);

        if (string.IsNullOrWhiteSpace(avatarRoute))
        {
            _debugLogHelper.LogError(nameof(CLETraderBootstrap), $"Unable to resolve avatar route for trader {traderBase.Id}");
            return;
        }

        imageRouter.AddRoute(avatarRoute, imagePath);
        _debugLogHelper.LogService(nameof(CLETraderBootstrap), $"Registered trader image route '{avatarRoute}' for trader {traderBase.Id}");
    }

    private static string NormalizeAvatarRoute(string? avatar)
    {
        if (string.IsNullOrWhiteSpace(avatar))
        {
            return string.Empty;
        }

        return avatar
            .Replace(".png", "", StringComparison.OrdinalIgnoreCase)
            .Replace(".jpg", "", StringComparison.OrdinalIgnoreCase)
            .Replace(".jpeg", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }
}