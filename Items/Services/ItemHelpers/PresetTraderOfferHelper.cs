using CommonLibExtended.Constants;
using CommonLibExtended.Core;
using CommonLibExtended.Helpers;
using CommonLibExtended.Items.Models;
using CommonLibExtended.Items.Services;
using CommonLibExtended.Items.Services.ItemHelpers.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Models;

namespace CommonLibExtended.Items.Services.ItemHelpers;

[Injectable]
public sealed class PresetTraderOfferHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    CLESettings settings,
    PresetBuildHelper presetBuildHelper,
    BuiltPresetCache builtPresetCache,
    PresetRegistryService presetRegistryService,
    TraderOfferHelper traderOfferHelper)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly DatabaseService _databaseService = databaseService;
    private readonly CLESettings _settings = settings;
    private readonly PresetBuildHelper _presetBuildHelper = presetBuildHelper;
    private readonly BuiltPresetCache _builtPresetCache = builtPresetCache;
    private readonly PresetRegistryService _presetRegistryService = presetRegistryService;
    private readonly TraderOfferHelper _traderOfferHelper = traderOfferHelper;

    public void Process(ItemModificationRequest request)
    {
        if (request?.Extras?.PresetTraders == null || request.Extras.PresetTraders.Count == 0)
        {
            return;
        }

        foreach (var (traderKey, assortEntries) in request.Extras.PresetTraders)
        {
            var traderId = ResolveTraderId(traderKey);
            if (string.IsNullOrWhiteSpace(traderId))
            {
                _debugLogHelper.LogError("PresetTraderOffer", $"Could not resolve trader '{traderKey}'");
                continue;
            }

            if (assortEntries == null || assortEntries.Count == 0)
            {
                continue;
            }

            foreach (var (sourceAssortId, config) in assortEntries)
            {
                if (!IsValidPresetTraderEntry(request, traderKey, sourceAssortId, config))
                {
                    continue;
                }

                var preset = _presetRegistryService.GetById(config!.PresetId);
                if (preset == null)
                {
                    _debugLogHelper.LogError(
                        "PresetTraderOffer",
                        $"Preset {config.PresetId} not found in preset registry for item {request.ItemId}");
                    continue;
                }

                var builtPreset = AddPresetOfferToTrader(traderId, sourceAssortId, preset, config);
                if (builtPreset == null)
                {
                    continue;
                }

                _builtPresetCache.Store(config.PresetId, sourceAssortId, builtPreset);
            }
        }
    }

    private BuiltPresetResult? AddPresetOfferToTrader(
        string traderId,
        string sourceAssortId,
        Preset preset,
        PresetTraderConfig config)
    {
        if (!_databaseService.GetTraders().TryGetValue(traderId, out var trader) || trader?.Assort == null)
        {
            _debugLogHelper.LogError("PresetTraderOffer", $"Trader {traderId} not found or assort is null");
            return null;
        }

        var builtPreset = _presetBuildHelper.BuildForTrader(preset, sourceAssortId, "PresetTraderOffer");
        if (builtPreset == null)
        {
            _debugLogHelper.LogError(
                "PresetTraderOffer",
                $"Failed to build preset {preset.Id} for trader={traderId}, sourceAssort={sourceAssortId}");
            return null;
        }

        var offerId = builtPreset.RootBuiltItemId?.ToString();
        if (string.IsNullOrWhiteSpace(offerId))
        {
            _debugLogHelper.LogError(
                "PresetTraderOffer",
                $"Built preset {preset.Id} returned invalid RootBuiltItemId for trader={traderId}, sourceAssort={sourceAssortId}");
            return null;
        }

        var applied = _traderOfferHelper.ApplyPresetOffer(
            trader.Assort,
            offerId,
            builtPreset.Items,
            config.Barters,
            config.LoyalLevelItems,
            "PresetTraderOffer",
            $"sourceAssort={sourceAssortId}, preset={preset.Id}, trader={traderId}");

        if (!applied)
        {
            _debugLogHelper.LogError(
                "PresetTraderOffer",
                $"Failed to apply preset offer offerId={offerId}, sourceAssort={sourceAssortId}, preset={preset.Id}, trader={traderId}");
            return null;
        }

        _debugLogHelper.LogService(
            "PresetTraderOffer",
            $"Added preset trader offer offerId={offerId}, sourceAssort={sourceAssortId}, preset={preset.Id}, trader={traderId}, itemCount={builtPreset.Items.Count}, rootOldId={builtPreset.RootSourceItemId}, rootNewId={builtPreset.RootBuiltItemId}");

        return builtPreset;
    }

    private bool IsValidPresetTraderEntry(
        ItemModificationRequest request,
        string traderKey,
        string sourceAssortId,
        PresetTraderConfig? config)
    {
        if (config == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(sourceAssortId))
        {
            _debugLogHelper.LogError(
                "PresetTraderOffer",
                $"Missing assortId for trader '{traderKey}' on item {request.ItemId}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.PresetId))
        {
            _debugLogHelper.LogError(
                "PresetTraderOffer",
                $"Missing presetId for trader '{traderKey}' assort '{sourceAssortId}'");
            return false;
        }

        return true;
    }

    private string? ResolveTraderId(string? traderKey)
    {
        if (_settings.ForceAllItemsToDefaultTrader)
        {
            return _settings.DefaultTraderId;
        }

        if (string.IsNullOrWhiteSpace(traderKey))
        {
            return _settings.DefaultTraderId;
        }

        if (Maps.TraderMap.TryGetValue(traderKey.ToLowerInvariant(), out var traderId))
        {
            return traderId;
        }

        return traderKey;
    }
}