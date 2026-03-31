using CommonLibExtended.Constants;
using CommonLibExtended.Core;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Models;

namespace CommonLibExtended.Helpers;

[Injectable]
public sealed class QuestAssortHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    CLESettings settings,
    PresetBuildHelper presetBuildHelper,
    BuiltPresetCache builtPresetCache)
{
    private const string StartedBucket = "started";
    private const string SuccessBucket = "success";
    private const string FailBucket = "fail";

    private const string RubTpl = "5449016a4bdc2d6f028b456f";
    private const string UsdTpl = "5696686a4bdc2da3298b456a";
    private const string EurTpl = "569668774bdc2da2298b4568";

    public void Process(ItemModificationRequest request)
    {
        if (request.Extras.QuestAssorts == null)
        {
            return;
        }

        foreach (var config in request.Extras.QuestAssorts)
        {
            if (config == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(config.QuestId))
            {
                debugLogHelper.LogError("QuestAssortHelper", $"Missing questId for item {request.ItemId}");
                continue;
            }

            var traderId = ResolveTraderId(config.TraderId);
            if (string.IsNullOrWhiteSpace(traderId))
            {
                debugLogHelper.LogError(
                    "QuestAssortHelper",
                    $"Could not resolve traderId for quest {config.QuestId} on item {request.ItemId}");
                continue;
            }

            var assortId = ResolveOrBuildAssortId(request, traderId, config);
            if (string.IsNullOrWhiteSpace(assortId))
            {
                debugLogHelper.LogError(
                    "QuestAssortHelper",
                    $"Could not resolve or build assortId for quest {config.QuestId} on item {request.ItemId}");
                continue;
            }

            AddQuestAssortMapping(
                traderId,
                config.QuestId,
                assortId,
                NormalizeQuestStatusBucket(config.Status));
        }
    }

    public void AddQuestAssortMapping(
        string traderId,
        string questId,
        string assortId,
        string statusBucket = SuccessBucket)
    {
        if (string.IsNullOrWhiteSpace(traderId))
        {
            debugLogHelper.LogError("QuestAssortHelper", "traderId is null or empty");
            return;
        }

        if (string.IsNullOrWhiteSpace(questId))
        {
            debugLogHelper.LogError("QuestAssortHelper", "questId is null or empty");
            return;
        }

        if (string.IsNullOrWhiteSpace(assortId))
        {
            debugLogHelper.LogError("QuestAssortHelper", $"assortId is null or empty for quest {questId}");
            return;
        }

        if (!databaseService.GetTraders().TryGetValue(traderId, out var trader) || trader?.Assort == null)
        {
            debugLogHelper.LogError("QuestAssortHelper", $"Trader {traderId} not found or assort is null");
            return;
        }

        var questAssort = trader.QuestAssort;
        if (questAssort == null)
        {
            debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Trader {traderId} has null QuestAssort and it cannot be assigned because the property is init-only");
            return;
        }

        EnsureQuestAssortBucket(questAssort, StartedBucket);
        EnsureQuestAssortBucket(questAssort, SuccessBucket);
        EnsureQuestAssortBucket(questAssort, FailBucket);

        var bucket = questAssort[statusBucket];
        bucket[assortId] = questId;

        debugLogHelper.LogService(
            "QuestAssortHelper",
            $"Added quest assort mapping trader={traderId} assort={assortId} -> quest={questId} ({statusBucket})");
    }

    private string? ResolveOrBuildAssortId(
        ItemModificationRequest request,
        string traderId,
        QuestAssortConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.AssortId))
        {
            if (EnsurePresetOfferExistsIfPossible(request, traderId, config.AssortId, config.PresetId))
            {
                return config.AssortId;
            }

            return config.AssortId;
        }

        if (string.IsNullOrWhiteSpace(config.PresetId))
        {
            return null;
        }

        var cached = builtPresetCache.GetByPresetId(config.PresetId);
        if (cached != null && !string.IsNullOrWhiteSpace(cached.RootBuiltItemId))
        {
            return cached.RootBuiltItemId;
        }

        var built = TryBuildPresetOfferFromRequest(request, traderId, config.PresetId);
        if (built != null && !string.IsNullOrWhiteSpace(built.RootBuiltItemId))
        {
            return built.RootBuiltItemId;
        }

        return null;
    }

    private bool EnsurePresetOfferExistsIfPossible(
        ItemModificationRequest request,
        string traderId,
        string assortId,
        string? presetId)
    {
        if (databaseService.GetTraders().TryGetValue(traderId, out var trader)
            && trader?.Assort?.Items != null
            && trader.Assort.Items.Any(x => string.Equals(x.Id, assortId, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(presetId))
        {
            var cached = builtPresetCache.GetByPresetId(presetId);
            if (cached != null)
            {
                return true;
            }

            var built = TryBuildPresetOfferFromRequest(request, traderId, presetId, assortId);
            return built != null;
        }

        return false;
    }

    private BuiltPresetResult? TryBuildPresetOfferFromRequest(
        ItemModificationRequest request,
        string traderId,
        string presetId,
        string? forcedAssortId = null)
    {
        if (request.Config.WeaponPresets == null || request.Config.WeaponPresets.Count == 0)
        {
            debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Cannot build preset {presetId}: request has no weapon presets");
            return null;
        }

        var preset = request.Config.WeaponPresets.FirstOrDefault(x =>
            string.Equals(x.Id, presetId, StringComparison.OrdinalIgnoreCase));

        if (preset == null)
        {
            debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Cannot build preset {presetId}: preset not found in request config");
            return null;
        }

        var presetTraderEntry = FindPresetTraderEntry(request, traderId, presetId, forcedAssortId);
        if (presetTraderEntry == null)
        {
            debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Cannot build preset {presetId}: no presetTraders entry found for trader {traderId}");
            return null;
        }

        var assortId = forcedAssortId ?? presetTraderEntry.Value.AssortId;
        var traderConfig = presetTraderEntry.Value.Config;

        if (!databaseService.GetTraders().TryGetValue(traderId, out var trader) || trader?.Assort == null)
        {
            debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Cannot build preset {presetId}: trader {traderId} not found or assort is null");
            return null;
        }

        var builtPreset = presetBuildHelper.BuildForTrader(preset, assortId, "QuestAssortHelper");
        if (builtPreset == null)
        {
            return null;
        }

        trader.Assort.Items ??= [];
        trader.Assort.BarterScheme ??= [];
        trader.Assort.LoyalLevelItems ??= [];

        var existingIds = new HashSet<string>(
            trader.Assort.Items.Select(x => x.Id.ToString()),
            StringComparer.OrdinalIgnoreCase);

        foreach (var item in builtPreset.Items)
        {
            if (!existingIds.Contains(item.Id.ToString()))
            {
                trader.Assort.Items.Add(item);
            }
        }

        trader.Assort.BarterScheme[assortId] = BuildBarterScheme(traderConfig.Barters);
        trader.Assort.LoyalLevelItems[assortId] = traderConfig.ConfigBarterSettings?.LoyalLevel ?? 1;

        builtPresetCache.Store(presetId, assortId, builtPreset);

        debugLogHelper.LogService(
            "QuestAssortHelper",
            $"Built missing preset offer for preset={presetId}, trader={traderId}, assort={assortId}");

        return builtPreset;
    }

    private (string AssortId, PresetTraderConfig Config)? FindPresetTraderEntry(
        ItemModificationRequest request,
        string traderId,
        string presetId,
        string? assortId = null)
    {
        if (request.Extras.PresetTraders == null || request.Extras.PresetTraders.Count == 0)
        {
            return null;
        }

        foreach (var (traderKey, assortEntries) in request.Extras.PresetTraders)
        {
            var resolvedTraderId = ResolveTraderId(traderKey);
            if (!string.Equals(resolvedTraderId, traderId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (assortEntries == null || assortEntries.Count == 0)
            {
                continue;
            }

            foreach (var (entryAssortId, entryConfig) in assortEntries)
            {
                if (entryConfig == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(assortId)
                    && !string.Equals(entryAssortId, assortId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(entryConfig.PresetId, presetId, StringComparison.OrdinalIgnoreCase))
                {
                    return (entryAssortId, entryConfig);
                }
            }
        }

        return null;
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

        if (Maps.TraderMap.TryGetValue(traderKey.ToLowerInvariant(), out var traderId))
        {
            return traderId;
        }

        return traderKey;
    }

    private static void EnsureQuestAssortBucket(
        Dictionary<string, Dictionary<MongoId, MongoId>?> questAssort,
        string bucketName)
    {
        if (!questAssort.ContainsKey(bucketName) || questAssort[bucketName] == null)
        {
            questAssort[bucketName] = new Dictionary<MongoId, MongoId>();
        }
    }

    private static string NormalizeQuestStatusBucket(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return SuccessBucket;
        }

        if (status.Equals("started", StringComparison.OrdinalIgnoreCase))
        {
            return StartedBucket;
        }

        if (status.Equals("fail", StringComparison.OrdinalIgnoreCase))
        {
            return FailBucket;
        }

        return SuccessBucket;
    }
}