using CommonCore.Constants;
using CommonCore.Core;
using CommonCore.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Models;
using static CommonCore.Models.CommonCoreItemConfig;
namespace CommonCore.Helpers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public sealed class PresetTraderOfferHelper(
    CoreDebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    CommonCoreSettings settings,
    PresetBuildHelper presetBuildHelper,
    BuiltPresetCache builtPresetCache)
{
    private const string RubTpl = "5449016a4bdc2d6f028b456f";
    private const string UsdTpl = "5696686a4bdc2da3298b456a";
    private const string EurTpl = "569668774bdc2da2298b4568";

    public void Process(CommonCoreItemRequest request)
    {
        if (request.Config.PresetTraders == null || request.Config.PresetTraders.Count == 0)
        {
            return;
        }

        if (request.Config.WeaponPresets == null)
        {
            debugLogHelper.LogError("PresetTraderOffer", $"Item {request.ItemId} has presetTraders but no presets");
            return;
        }

        foreach (var (traderKey, assortEntries) in request.Config.PresetTraders)
        {
            var traderId = ResolveTraderId(traderKey);
            if (string.IsNullOrWhiteSpace(traderId))
            {
                debugLogHelper.LogError("PresetTraderOffer", $"Could not resolve trader '{traderKey}'");
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

                if (string.IsNullOrWhiteSpace(config.PresetId))
                {
                    debugLogHelper.LogError("PresetTraderOffer", $"Missing presetId for trader '{traderKey}' assort '{assortId}'");
                    continue;
                }

                var preset = request.Config.WeaponPresets.FirstOrDefault(x =>
                    string.Equals(x.Id, config.PresetId, StringComparison.OrdinalIgnoreCase));

                if (preset == null)
                {
                    debugLogHelper.LogError("PresetTraderOffer", $"Preset {config.PresetId} not found in request.Presets");
                    continue;
                }

                var builtPreset = AddPresetOfferToTrader(traderId, assortId, preset, config);
                if (builtPreset != null)
                {
                    builtPresetCache.Store(config.PresetId, assortId, builtPreset);
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
        if (!databaseService.GetTraders().TryGetValue(traderId, out var trader) || trader?.Assort == null)
        {
            debugLogHelper.LogError("PresetTraderOffer", $"Trader {traderId} not found or assort is null");
            return null;
        }

        var builtPreset = presetBuildHelper.BuildForTrader(preset, assortId, "PresetTraderOffer");
        if (builtPreset == null)
        {
            return null;
        }

        trader.Assort.Items ??= [];
        trader.Assort.BarterScheme ??= [];
        trader.Assort.LoyalLevelItems ??= [];

        foreach (var item in builtPreset.Items)
        {
            trader.Assort.Items.Add(item);

            debugLogHelper.LogService(
                "PresetTraderOffer",
                $"Added trader assort item: Id={item.Id}, ParentId={item.ParentId}, Template={item.Template}, SlotId={item.SlotId}");
        }

        trader.Assort.BarterScheme[assortId] = BuildBarterScheme(config.Barters);
        trader.Assort.LoyalLevelItems[assortId] = config.ConfigBarterSettings?.LoyalLevel ?? 1;

        debugLogHelper.LogService(
            "PresetTraderOffer",
            $"Added preset trader offer assort={assortId}, preset={preset.Id}, trader={traderId}, itemCount={builtPreset.Items.Count}, rootOldId={builtPreset.RootSourceItemId}, rootNewId={builtPreset.RootBuiltItemId}");

        return builtPreset;
    }

    private List<List<BarterScheme>> BuildBarterScheme(List<ConfigBarterScheme>? config)
    {
        if (config == null || config.Count == 0)
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

        var row = new List<BarterScheme>();

        foreach (var entry in config)
        {
            row.Add(new BarterScheme
            {
                Template = NormalizeCurrencyOrTpl(entry.Template),
                Count = entry.Count
            });
        }

        return [row];
    }

    private static string NormalizeCurrencyOrTpl(string value)
    {
        if (value.Equals("RUB", StringComparison.OrdinalIgnoreCase)) return RubTpl;
        if (value.Equals("USD", StringComparison.OrdinalIgnoreCase)) return UsdTpl;
        if (value.Equals("EUR", StringComparison.OrdinalIgnoreCase)) return EurTpl;
        return value;
    }

    private string? ResolveTraderId(string? traderKey)
    {
        if (settings.ForceAllItemsToDefaultTrader)
        {
            return settings.DefaultTraderId;
        }

        if (string.IsNullOrWhiteSpace(traderKey))
        {
            return settings.DefaultTraderId;
        }

        if (ItemMaps.TraderMap.TryGetValue(traderKey.ToLowerInvariant(), out var traderId))
        {
            return traderId;
        }

        return traderKey;
    }
}