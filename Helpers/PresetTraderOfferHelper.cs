using CommonLibExtended.Constants;
using CommonLibExtended.Core;
using CommonLibExtended.Models;
using CommonLibExtended.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Models;

namespace CommonLibExtended.Helpers;

[Injectable]
public sealed class PresetTraderOfferHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    CLESettings settings,
    PresetBuildHelper presetBuildHelper,
    BuiltPresetCache builtPresetCache,
    PresetRegistryService presetRegistryService)
{
    private const string RubTpl = "5449016a4bdc2d6f028b456f";
    private const string UsdTpl = "5696686a4bdc2da3298b456a";
    private const string EurTpl = "569668774bdc2da2298b4568";

    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly DatabaseService _databaseService = databaseService;
    private readonly CLESettings _settings = settings;
    private readonly PresetBuildHelper _presetBuildHelper = presetBuildHelper;
    private readonly BuiltPresetCache _builtPresetCache = builtPresetCache;
    private readonly PresetRegistryService _presetRegistryService = presetRegistryService;

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

            foreach (var (assortId, config) in assortEntries)
            {
                if (config == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(assortId))
                {
                    _debugLogHelper.LogError(
                        "PresetTraderOffer",
                        $"Missing assortId for trader '{traderKey}' on item {request.ItemId}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(config.PresetId))
                {
                    _debugLogHelper.LogError(
                        "PresetTraderOffer",
                        $"Missing presetId for trader '{traderKey}' assort '{assortId}'");
                    continue;
                }

                var preset = _presetRegistryService.GetById(config.PresetId);
                if (preset == null)
                {
                    _debugLogHelper.LogError(
                        "PresetTraderOffer",
                        $"Preset {config.PresetId} not found in preset registry for item {request.ItemId}");
                    continue;
                }

                var builtPreset = AddPresetOfferToTrader(traderId, assortId, preset, config);
                if (builtPreset != null)
                {
                    _builtPresetCache.Store(config.PresetId, assortId, builtPreset);
                }
            }
        }
    }

    private BuiltPresetResult? AddPresetOfferToTrader(
        string traderId,
        string assortId,
        Preset preset,
        PresetTraderConfig config)
    {
        if (!_databaseService.GetTraders().TryGetValue(traderId, out var trader) || trader?.Assort == null)
        {
            _debugLogHelper.LogError("PresetTraderOffer", $"Trader {traderId} not found or assort is null");
            return null;
        }

        var builtPreset = _presetBuildHelper.BuildForTrader(preset, assortId, "PresetTraderOffer");
        if (builtPreset == null)
        {
            _debugLogHelper.LogError(
                "PresetTraderOffer",
                $"Failed to build preset {preset.Id} for trader {traderId} assort {assortId}");
            return null;
        }

        trader.Assort.Items ??= [];
        trader.Assort.BarterScheme ??= [];
        trader.Assort.LoyalLevelItems ??= [];

        foreach (var item in builtPreset.Items)
        {
            trader.Assort.Items.Add(item);

            _debugLogHelper.LogService(
                "PresetTraderOffer",
                $"Added trader assort item: Id={item.Id}, ParentId={item.ParentId}, Template={item.Template}, SlotId={item.SlotId}");
        }

        _debugLogHelper.LogService(
            "PresetTraderOffer",
            $"Finished adding items for preset {preset.Id} to trader {traderId}, now configuring barter scheme");

        var offerId = builtPreset.RootBuiltItemId?.ToString();

        if (string.IsNullOrWhiteSpace(offerId))
        {
            _debugLogHelper.LogError(
                "PresetTraderOffer",
                $"Invalid RootBuiltItemId for preset {preset.Id} on trader {traderId}");
            return null;
        }

        var barter = BuildBarterScheme(config.Barters);

        trader.Assort.BarterScheme[offerId] = barter;
        trader.Assort.LoyalLevelItems[offerId] = config.ConfigBarterSettings?.LoyalLevel ?? 1;

        _debugLogHelper.LogService(
            "PresetTraderOffer",
            $"Added preset trader offer assort={offerId}, sourceAssort={assortId}, preset={preset.Id}, trader={traderId}, itemCount={builtPreset.Items.Count}, rootOldId={builtPreset.RootSourceItemId}, rootNewId={builtPreset.RootBuiltItemId}");

        return builtPreset;
    }

    private static List<List<BarterScheme>> BuildBarterScheme(List<ConfigBarterScheme>? config)
    {
        if (config == null || config.Count == 0)
        {
            return CreateDefaultBarterScheme();
        }

        var row = new List<BarterScheme>();

        foreach (var entry in config)
        {
            if (entry == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Template))
            {
                continue;
            }

            if (entry.Count <= 0)
            {
                continue;
            }

            row.Add(new BarterScheme
            {
                Template = NormalizeCurrencyOrTpl(entry.Template),
                Count = entry.Count
            });
        }

        return row.Count > 0
            ? [row]
            : CreateDefaultBarterScheme();
    }

    private static List<List<BarterScheme>> CreateDefaultBarterScheme()
    {
        return
        [
            [
            new BarterScheme
            {
                Template = RubTpl,
                Count = 1
            }
        ]
        ];
    }

    private static string NormalizeCurrencyOrTpl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return RubTpl;
        }

        if (value.Equals("RUB", StringComparison.OrdinalIgnoreCase)) return RubTpl;
        if (value.Equals("USD", StringComparison.OrdinalIgnoreCase)) return UsdTpl;
        if (value.Equals("EUR", StringComparison.OrdinalIgnoreCase)) return EurTpl;

        return value;
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