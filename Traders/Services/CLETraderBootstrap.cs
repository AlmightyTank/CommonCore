using CommonLibExtended.Helpers;
using CommonLibExtended.Services;
using CommonLibExtended.Traders.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

namespace CommonLibExtended.Traders.Services;

[Injectable(InjectionType.Singleton)]
public sealed class CLETraderBootstrap(
    DebugLogHelper debugLogHelper,
    ModPathHelper modPathHelper,
    CustomTraderLoader customTraderLoader,
    CustomTraderUnlockService customTraderUnlockService,
    CustomTraderPricingRegistry customTraderPricingRegistry,
    ImageRouter imageRouter,
    JsonUtil jsonUtil)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly ModPathHelper _modPathHelper = modPathHelper;
    private readonly CustomTraderLoader _customTraderLoader = customTraderLoader;
    private readonly CustomTraderUnlockService _customTraderUnlockService = customTraderUnlockService;
    private readonly CustomTraderPricingRegistry _customTraderPricingRegistry = customTraderPricingRegistry;
    private readonly ImageRouter _imageRouter = imageRouter;
    private readonly JsonUtil _jsonUtil = jsonUtil;

    public void LoadTrader(
        Assembly assembly,
        string traderBaseRelativePath,
        string assortRelativePath,
        string settingsRelativePath,
        string? firstName = null,
        string description = "",
        string? traderImageRelativePath = null,
        string? avatarRouteOverride = null)
    {
        var traderBasePath = _modPathHelper.GetFullPath(assembly, traderBaseRelativePath);
        var assortPath = _modPathHelper.GetFullPath(assembly, assortRelativePath);
        var settingsPath = _modPathHelper.GetFullPath(assembly, settingsRelativePath);

        var traderBase = LoadRequired<TraderBase>(traderBasePath, "trader base");
        var assort = LoadRequired<TraderAssort>(assortPath, "trader assort");
        var settings = LoadRequired<CustomTraderSettings>(settingsPath, "trader settings");

        settings.Validate(traderBase.Id);

        RegisterTraderImageIfProvided(
            assembly,
            traderBase,
            traderImageRelativePath,
            avatarRouteOverride);

        _customTraderLoader.LoadTrader(
            traderBase,
            assort,
            settings,
            firstName ?? traderBase.Nickname ?? traderBase.Name ?? "Trader",
            description);

        _customTraderUnlockService.RegisterTrader(
            traderBase.Id,
            settings.MinLevel,
            settings.UnlockedByDefault);

        _customTraderPricingRegistry.Register(traderBase.Id, settings);

        _debugLogHelper.LogService(nameof(CLETraderBootstrap), $"Loaded custom trader {traderBase.Id}");
    }

    private T LoadRequired<T>(string path, string label) where T : class
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"{label} file not found: {path}", path);
        }

        var result = _jsonUtil.Deserialize<T>(File.ReadAllText(path));
        return result ?? throw new InvalidDataException($"Failed to deserialize {label}: {path}");
    }

    private void RegisterTraderImageIfProvided(
        Assembly assembly,
        TraderBase traderBase,
        string? traderImageRelativePath,
        string? avatarRouteOverride)
    {
        if (string.IsNullOrWhiteSpace(traderImageRelativePath))
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

        _imageRouter.AddRoute(avatarRoute, imagePath);
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