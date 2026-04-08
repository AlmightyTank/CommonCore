using CommonLibExtended.Constants;
using CommonLibExtended.Core;
using CommonLibExtended.Helpers;
using CommonLibExtended.Items.Services.ItemHelpers.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Models;

namespace CommonLibExtended.Items.Services.ItemHelpers;

[Injectable]
public sealed class QuestAssortHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    CLESettings settings,
    BuiltPresetCache builtPresetCache,
    TraderOfferHelper traderOfferHelper,
    PresetBuildHelper presetBuildHelper,
    PresetRegistryService presetRegistryService,
    StandardTraderItemBuildHelper standardTraderItemBuildHelper)
{
    private const string StartedBucket = "started";
    private const string SuccessBucket = "success";
    private const string FailBucket = "fail";

    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly DatabaseService _databaseService = databaseService;
    private readonly CLESettings _settings = settings;
    private readonly BuiltPresetCache _builtPresetCache = builtPresetCache;
    private readonly TraderOfferHelper _traderOfferHelper = traderOfferHelper;
    private readonly PresetBuildHelper _presetBuildHelper = presetBuildHelper;
    private readonly PresetRegistryService _presetRegistryService = presetRegistryService;
    private readonly StandardTraderItemBuildHelper _standardTraderItemBuildHelper = standardTraderItemBuildHelper;

    public void Process(ItemModificationRequest request)
    {
        if (request?.Extras?.QuestAssorts == null || request.Extras.QuestAssorts.Length == 0)
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
                _debugLogHelper.LogError("QuestAssortHelper", $"Missing questId for item {request.ItemId}");
                continue;
            }

            var traderId = ResolveTraderId(config.TraderId);
            if (string.IsNullOrWhiteSpace(traderId))
            {
                _debugLogHelper.LogError(
                    "QuestAssortHelper",
                    $"Could not resolve traderId for quest {config.QuestId} on item {request.ItemId}");
                continue;
            }

            var assortId = ResolveFinalAssortId(request, config);
            if (string.IsNullOrWhiteSpace(assortId))
            {
                _debugLogHelper.LogError(
                    "QuestAssortHelper",
                    $"Could not resolve final assortId for quest {config.QuestId} on item {request.ItemId}");
                continue;
            }

            var normalizedBucket = NormalizeQuestStatusBucket(config.Status);

            if (!EnsureOfferExistsForQuest(request, config, traderId, assortId))
            {
                _debugLogHelper.LogError(
                    "QuestAssortHelper",
                    $"Unable to ensure offer exists for trader={traderId}, assort={assortId}, quest={config.QuestId}");
                continue;
            }

            var added = AddQuestAssortMapping(
                traderId,
                config.QuestId,
                assortId,
                normalizedBucket);

            if (!added)
            {
                _debugLogHelper.LogError(
                    "QuestAssortHelper",
                    $"Failed to add quest assort mapping for trader={traderId}, assort={assortId}, quest={config.QuestId}");
                continue;
            }

            if (!TryGetOfferDisplayData(traderId, assortId, out var template, out var loyaltyLevel))
            {
                _debugLogHelper.LogError(
                    "QuestAssortHelper",
                    $"Failed to resolve offer display data for trader={traderId}, assort={assortId}, quest={config.QuestId}");
                continue;
            }

            AddQuestAssortRewardDisplay(
                config.QuestId,
                traderId,
                assortId,
                normalizedBucket,
                template,
                loyaltyLevel);
        }
    }

    public bool AddQuestAssortMapping(
        string traderId,
        string questId,
        string assortId,
        string statusBucket = SuccessBucket)
    {
        if (string.IsNullOrWhiteSpace(traderId))
        {
            _debugLogHelper.LogError("QuestAssortHelper", "traderId is null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(questId))
        {
            _debugLogHelper.LogError("QuestAssortHelper", "questId is null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(assortId))
        {
            _debugLogHelper.LogError("QuestAssortHelper", $"assortId is null or empty for quest {questId}");
            return false;
        }

        if (!_databaseService.GetTraders().TryGetValue(traderId, out var trader) || trader?.Assort == null)
        {
            _debugLogHelper.LogError("QuestAssortHelper", $"Trader {traderId} not found or assort is null");
            return false;
        }

        if (!TraderHasValidAssortEntry(traderId, assortId))
        {
            _debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Assort {assortId} is not fully present in trader {traderId} assort DB. Refusing to add quest mapping.");
            return false;
        }

        var questAssort = trader.QuestAssort;
        if (questAssort == null)
        {
            _debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Trader {traderId} has null QuestAssort and it cannot be assigned because the property is init-only");
            return false;
        }

        var normalizedBucket = NormalizeQuestStatusBucket(statusBucket);

        EnsureQuestAssortBucket(questAssort, StartedBucket);
        EnsureQuestAssortBucket(questAssort, SuccessBucket);
        EnsureQuestAssortBucket(questAssort, FailBucket);

        var assortMongoId = new MongoId(assortId);
        var questMongoId = new MongoId(questId);

        var bucket = questAssort[normalizedBucket]!;
        _debugLogHelper.LogService(
            "QuestAssortHelper",
            $"Bucket '{normalizedBucket}' count before add: {bucket.Count}");

        bucket[assortMongoId] = questMongoId;

        var verified =
            bucket.TryGetValue(assortMongoId, out var storedQuestId) &&
            storedQuestId.Equals(questMongoId);

        if (!verified)
        {
            _debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Failed to verify quest assort mapping after add for trader={traderId} assort={assortId} -> quest={questId} ({normalizedBucket})");
            return false;
        }

        _debugLogHelper.LogService(
            "QuestAssortHelper",
            $"Verified quest assort mapping trader={traderId} assort={assortId} -> quest={questId} ({normalizedBucket})");

        _debugLogHelper.LogService(
            "QuestAssortHelper",
            $"Bucket '{normalizedBucket}' count after add: {bucket.Count}");

        return true;
    }

    private bool EnsureOfferExistsForQuest(
        ItemModificationRequest request,
        QuestAssortConfig config,
        string traderId,
        string assortId)
    {
        if (TraderHasValidAssortEntry(traderId, assortId))
        {
            _debugLogHelper.LogService(
                "QuestAssortHelper",
                $"Offer already exists for trader={traderId}, assort={assortId}");
            return true;
        }

        _debugLogHelper.LogService(
            "QuestAssortHelper",
            $"Offer missing for trader={traderId}, assort={assortId}. Attempting rebuild from item config.");

        var rebuilt = TryRebuildOfferFromItemJson(request, config, traderId, assortId);
        if (!rebuilt)
        {
            _debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Failed rebuild attempt for trader={traderId}, assort={assortId}");
            return false;
        }

        if (!TraderHasValidAssortEntry(traderId, assortId))
        {
            _debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Offer still missing after rebuild for trader={traderId}, assort={assortId}");
            return false;
        }

        _debugLogHelper.LogService(
            "QuestAssortHelper",
            $"Offer rebuild verified for trader={traderId}, assort={assortId}");

        return true;
    }

    private bool TryRebuildOfferFromItemJson(
        ItemModificationRequest request,
        QuestAssortConfig config,
        string traderId,
        string assortId)
    {
        if (TryRebuildPresetOfferFromItemJson(request, config, traderId, assortId))
        {
            return true;
        }

        if (TryRebuildStandardOfferFromItemJson(request, traderId, assortId))
        {
            return true;
        }

        return false;
    }

    private bool TryRebuildPresetOfferFromItemJson(
        ItemModificationRequest request,
        QuestAssortConfig config,
        string traderId,
        string assortId)
    {
        if (request?.Extras?.PresetTraders == null || request.Extras.PresetTraders.Count == 0)
        {
            return false;
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

            foreach (var (sourceAssortId, presetConfig) in assortEntries)
            {
                if (presetConfig == null || string.IsNullOrWhiteSpace(presetConfig.PresetId))
                {
                    continue;
                }

                var cachedFinalAssortId = _builtPresetCache.ResolveFinalOfferId(sourceAssortId);

                var assortMatches =
                    string.Equals(sourceAssortId, assortId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(cachedFinalAssortId, assortId, StringComparison.OrdinalIgnoreCase);

                var presetMatches =
                    !string.IsNullOrWhiteSpace(config.PresetId) &&
                    string.Equals(config.PresetId, presetConfig.PresetId, StringComparison.OrdinalIgnoreCase);

                if (!assortMatches && !presetMatches)
                {
                    continue;
                }

                var preset = _presetRegistryService.GetById(presetConfig.PresetId);
                if (preset == null)
                {
                    _debugLogHelper.LogError(
                        "QuestAssortHelper",
                        $"Preset {presetConfig.PresetId} not found for trader={traderId}, assort={assortId}");
                    return false;
                }

                var builtPreset = _presetBuildHelper.BuildForTrader(preset, sourceAssortId, "QuestAssortHelper");
                if (builtPreset == null)
                {
                    _debugLogHelper.LogError(
                        "QuestAssortHelper",
                        $"Failed to build preset {preset.Id} for trader={traderId}, assort={assortId}");
                    return false;
                }

                var offerId = builtPreset.RootBuiltItemId?.ToString();
                if (string.IsNullOrWhiteSpace(offerId))
                {
                    _debugLogHelper.LogError(
                        "QuestAssortHelper",
                        $"Built preset {preset.Id} returned invalid offerId for trader={traderId}");
                    return false;
                }

                if (!_databaseService.GetTraders().TryGetValue(traderId, out var trader) || trader?.Assort == null)
                {
                    _debugLogHelper.LogError(
                        "QuestAssortHelper",
                        $"Trader {traderId} not found while rebuilding preset offer {offerId}");
                    return false;
                }

                var applied = _traderOfferHelper.ApplyPresetOffer(
                    trader.Assort,
                    offerId,
                    builtPreset.Items,
                    presetConfig.Barters,
                    presetConfig.LoyalLevelItems,
                    "QuestAssortHelper",
                    $"preset rebuild trader={traderId}, sourceAssort={sourceAssortId}, offerId={offerId}, preset={preset.Id}");

                if (!applied)
                {
                    return false;
                }

                DumpQuestOfferState(traderId, offerId);

                _builtPresetCache.Store(preset.Id, sourceAssortId, builtPreset);

                return string.Equals(offerId, assortId, StringComparison.OrdinalIgnoreCase)
                    || TraderHasValidAssortEntry(traderId, assortId)
                    || TraderHasValidAssortEntry(traderId, offerId);
            }
        }

        return false;
    }

    private bool TryRebuildStandardOfferFromItemJson(
        ItemModificationRequest request,
        string traderId,
        string assortId)
    {
        if (request?.Config?.Traders == null || request.Config.Traders.Count == 0)
        {
            return false;
        }

        if (!_databaseService.GetTraders().TryGetValue(traderId, out var trader) || trader?.Assort == null)
        {
            _debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Trader {traderId} not found while rebuilding standard offer {assortId}");
            return false;
        }

        foreach (var (traderKey, assortConfigs) in request.Config.Traders)
        {
            var resolvedTraderId = ResolveTraderId(traderKey);
            if (!string.Equals(resolvedTraderId, traderId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (assortConfigs == null || assortConfigs.Count == 0)
            {
                continue;
            }

            foreach (var (configAssortId, traderConfig) in assortConfigs)
            {
                if (traderConfig == null)
                {
                    continue;
                }

                var offerMatches =
                    string.Equals(configAssortId, assortId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(request.ItemId, assortId, StringComparison.OrdinalIgnoreCase);

                if (!offerMatches)
                {
                    continue;
                }

                var offerSettings = MapTraderOfferSettings(traderConfig.ConfigBarterSettings);

                var rootItem = _standardTraderItemBuildHelper.BuildItemsForTrader(
                    request,
                    configAssortId,
                    offerSettings);

                if (rootItem == null)
                {
                    _debugLogHelper.LogError(
                        "QuestAssortHelper",
                        $"Standard trader build returned null root item for trader={traderId}, assort={configAssortId}");
                    return false;
                }

                var applied = _traderOfferHelper.ApplySingleItemOffer(
                    trader.Assort,
                    configAssortId,
                    rootItem,
                    traderConfig.Barters,
                    offerSettings,
                    "QuestAssortHelper",
                    $"standard rebuild trader={traderId}, assort={configAssortId}, item={request.ItemId}");

                if (!applied)
                {
                    return false;
                }

                DumpQuestOfferState(traderId, configAssortId);

                return true;
            }
        }

        _debugLogHelper.LogService(
            "QuestAssortHelper",
            $"No matching standard trader config found for trader={traderId}, assort={assortId}");

        return false;
    }

    private void DumpQuestOfferState(string traderId, string assortId)
    {
        if (!_databaseService.GetTraders().TryGetValue(traderId, out var trader) || trader?.Assort == null)
        {
            return;
        }

        var collectedItems = CollectOfferItems(trader.Assort.Items ?? [], assortId);

        List<BarterScheme> barter = [];
        if (trader.Assort.BarterScheme != null &&
            trader.Assort.BarterScheme.TryGetValue(new MongoId(assortId), out var barterGroups) &&
            barterGroups != null)
        {
            barter = barterGroups.SelectMany(x => x ?? []).ToList();
        }

        int? loyal = null;
        if (trader.Assort.LoyalLevelItems != null &&
            trader.Assort.LoyalLevelItems.TryGetValue(assortId, out var loyalValue))
        {
            loyal = loyalValue;
        }

        _debugLogHelper.LogService(
            "QuestAssortHelper",
            $"QUEST DEBUG OFFER\n" +
            $"Trader={traderId}, Assort={assortId}\n" +
            $"ITEMS:\n{DebugDumpHelper.DumpItems(collectedItems)}\n" +
            $"BARTERS:\n{DebugDumpHelper.DumpItems(barter)}\n" +
            $"LOYAL LEVEL:\n{DebugDumpHelper.DumpItems(loyal)}\n" +
            $"--- END QUEST DEBUG ---");
    }

    private static List<Item> CollectOfferItems(List<Item> allItems, string rootAssortId)
    {
        var collected = new List<Item>();
        var queue = new Queue<string>();
        queue.Enqueue(rootAssortId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            var matches = allItems
                .Where(x =>
                    string.Equals(x.Id.ToString(), currentId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.ParentId?.ToString(), currentId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var item in matches)
            {
                if (collected.Any(x => x.Id.Equals(item.Id)))
                {
                    continue;
                }

                collected.Add(item);
                queue.Enqueue(item.Id.ToString());
            }
        }

        return collected;
    }

    private bool TraderHasValidAssortEntry(string traderId, string assortId)
    {
        if (!_databaseService.GetTraders().TryGetValue(traderId, out var trader) || trader?.Assort == null)
        {
            return false;
        }

        var assortMongoId = new MongoId(assortId);

        var existsInItems = trader.Assort.Items != null &&
                            trader.Assort.Items.Any(x => x.Id.Equals(assortMongoId));

        var existsInBarter = trader.Assort.BarterScheme != null &&
                             trader.Assort.BarterScheme.ContainsKey(assortMongoId);

        var existsInLoyal = trader.Assort.LoyalLevelItems != null &&
                            trader.Assort.LoyalLevelItems.ContainsKey(assortId);

        _debugLogHelper.LogService(
            "QuestAssortHelper",
            $"Assort validation trader={traderId}, assort={assortId}: items={existsInItems}, barter={existsInBarter}, loyal={existsInLoyal}");

        return existsInItems && existsInBarter && existsInLoyal;
    }

    private bool TryGetOfferDisplayData(
        string traderId,
        string assortId,
        out string template,
        out int loyaltyLevel)
    {
        template = string.Empty;
        loyaltyLevel = 1;

        if (!_databaseService.GetTraders().TryGetValue(traderId, out var trader) || trader?.Assort == null)
        {
            return false;
        }

        var assortMongoId = new MongoId(assortId);

        var rootItem = trader.Assort.Items?
            .FirstOrDefault(x => x.Id.Equals(assortMongoId));

        if (rootItem == null)
        {
            return false;
        }

        template = rootItem.Template.ToString();

        if (trader.Assort.LoyalLevelItems != null &&
            trader.Assort.LoyalLevelItems.TryGetValue(assortId, out var resolvedLoyaltyLevel))
        {
            loyaltyLevel = resolvedLoyaltyLevel;
        }

        return !string.IsNullOrWhiteSpace(template);
    }

    private void AddQuestAssortRewardDisplay(
        string questId,
        string traderId,
        string assortId,
        string status,
        string template,
        int loyaltyLevel)
    {
        var quests = _databaseService.GetTables().Templates.Quests;

        if (!quests.TryGetValue(questId, out var quest) || quest == null)
        {
            _debugLogHelper.LogError("QuestAssortHelper", $"Quest {questId} not found");
            return;
        }

        if (!_databaseService.GetTraders().TryGetValue(traderId, out var trader) || trader?.Assort == null)
        {
            _debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Trader {traderId} not found or assort is null while building quest reward display for assort {assortId}");
            return;
        }

        quest.Rewards ??= new Dictionary<string, List<Reward>>(StringComparer.OrdinalIgnoreCase);

        var bucket = NormalizeRewardBucket(status);

        quest.Rewards.TryAdd("Started", []);
        quest.Rewards.TryAdd("Success", []);
        quest.Rewards.TryAdd("Fail", []);

        var rewards = quest.Rewards[bucket];
        if (rewards == null)
        {
            _debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Reward bucket {bucket} is null for quest {questId}");
            return;
        }

        if (rewards.Any(x =>
                x != null &&
                x.Type == RewardType.AssortmentUnlock &&
                string.Equals(x.Target, assortId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)x.TraderId, traderId, StringComparison.OrdinalIgnoreCase)))
        {
            _debugLogHelper.LogService(
                "QuestAssortHelper",
                $"Quest assort UI reward already exists for trader={traderId}, assort={assortId}, quest={questId}, bucket={bucket}");
            return;
        }

        var rewardItems = BuildQuestRewardItemsFromTraderAssort(trader.Assort, assortId);
        if (rewardItems.Count == 0)
        {
            _debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Failed to build quest reward items from trader assort for trader={traderId}, assort={assortId}");
            return;
        }

        var reward = new Reward
        {
            AvailableInGameEditions = [],
            Id = new MongoId(),
            Index = rewards.Count,
            Type = RewardType.AssortmentUnlock,
            Target = assortId,
            TraderId = traderId,
            Value = 1,
            Items = rewardItems,
            LoyaltyLevel = loyaltyLevel
        };

        rewards.Add(reward);

        var verified = rewards.Any(x =>
            x != null &&
            x.Type == RewardType.AssortmentUnlock &&
            string.Equals(x.Target, assortId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals((string?)x.TraderId, traderId, StringComparison.OrdinalIgnoreCase));

        if (!verified)
        {
            _debugLogHelper.LogError(
                "QuestAssortHelper",
                $"Failed to verify quest assort UI reward after add for trader={traderId}, assort={assortId}, quest={questId}, bucket={bucket}");
            return;
        }

        _debugLogHelper.LogService(
            "QuestAssortHelper",
            $"Added and verified quest assort UI reward: quest={questId}, trader={traderId}, assort={assortId}, bucket={bucket}, loyalty={loyaltyLevel}, template={template}, rewardItemCount={rewardItems.Count}");
    }

    private List<Item> BuildQuestRewardItemsFromTraderAssort(TraderAssort assort, string assortId)
    {
        if (assort?.Items == null || assort.Items.Count == 0)
        {
            return [];
        }

        var sourceItems = CollectOfferItems(assort.Items, assortId);
        if (sourceItems.Count == 0)
        {
            return [];
        }

        return CloneItemsForQuestReward(sourceItems);
    }

    private static List<Item> CloneItemsForQuestReward(List<Item> sourceItems)
    {
        return sourceItems
            .Select(CloneItemForQuestReward)
            .ToList();
    }

    private static Item CloneItemForQuestReward(Item source)
    {
        return new Item
        {
            Id = source.Id,
            Template = source.Template,
            ParentId = source.ParentId,
            SlotId = source.SlotId,
            Location = source.Location,
            Upd = source.Upd,
            Desc = source.Desc
        };
    }

    private string? ResolveFinalAssortId(ItemModificationRequest request, QuestAssortConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.AssortId))
        {
            var resolved = _builtPresetCache.ResolveFinalOfferId(config.AssortId);

            if (!string.Equals(resolved, config.AssortId, StringComparison.OrdinalIgnoreCase))
            {
                _debugLogHelper.LogService(
                    "QuestAssortHelper",
                    $"Resolved config assortId {config.AssortId} -> built root {resolved}");
            }

            return resolved;
        }

        if (!string.IsNullOrWhiteSpace(config.PresetId))
        {
            var resolved = _builtPresetCache.ResolveFinalOfferIdFromPresetId(config.PresetId);

            if (!string.IsNullOrWhiteSpace(resolved))
            {
                _debugLogHelper.LogService(
                    "QuestAssortHelper",
                    $"Resolved presetId {config.PresetId} -> built root {resolved}");
                return resolved;
            }

            _debugLogHelper.LogService(
                "QuestAssortHelper",
                $"Preset {config.PresetId} was not yet found in built preset cache for item {request.ItemId}. Falling back to rebuild path.");

            return !string.IsNullOrWhiteSpace(config.AssortId)
                ? config.AssortId
                : request.ItemId;
        }

        if (!string.IsNullOrWhiteSpace(request.ItemId))
        {
            var resolved = _builtPresetCache.ResolveFinalOfferId(request.ItemId);

            if (!string.Equals(resolved, request.ItemId, StringComparison.OrdinalIgnoreCase))
            {
                _debugLogHelper.LogService(
                    "QuestAssortHelper",
                    $"Resolved request itemId {request.ItemId} -> built root {resolved}");
            }

            return resolved;
        }

        return null;
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

    private static TraderOfferSettings? MapTraderOfferSettings(ConfigBarterSettings? source)
    {
        if (source == null)
        {
            return null;
        }

        return new TraderOfferSettings
        {
            LoyalLevel = source.LoyalLevel,
            UnlimitedCount = source.UnlimitedCount,
            StackObjectsCount = source.StackObjectsCount,
            BuyRestrictionMax = source.BuyRestrictionMax ?? 0
        };
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

    private static string NormalizeRewardBucket(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "Success";
        }

        if (status.Equals("started", StringComparison.OrdinalIgnoreCase))
        {
            return "Started";
        }

        if (status.Equals("fail", StringComparison.OrdinalIgnoreCase))
        {
            return "Fail";
        }

        return "Success";
    }
}