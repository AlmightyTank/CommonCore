using CommonLibExtended.Helpers;
using CommonLibExtended.Items.Services.ItemHelpers.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;

namespace CommonLibExtended.Items.Services.ItemHelpers;

[Injectable]
public sealed class QuestRewardHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    BuiltPresetCache builtPresetCache)
{
    private const string StartedBucket = "Started";
    private const string SuccessBucket = "Success";
    private const string FailBucket = "Fail";

    private const string RubTpl = "5449016a4bdc2d6f028b456f";
    private const string UsdTpl = "5696686a4bdc2da3298b456a";
    private const string EurTpl = "569668774bdc2da2298b4568";

    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly DatabaseService _databaseService = databaseService;
    private readonly BuiltPresetCache _builtPresetCache = builtPresetCache;

    public void Process(ItemModificationRequest request)
    {
        if (request.Extras?.QuestRewards == null || request.Extras.QuestRewards.Count == 0)
        {
            return;
        }

        foreach (var config in request.Extras.QuestRewards)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.QuestId))
            {
                _debugLogHelper.LogError("QuestReward", $"Missing questId for item {request.ItemId}");
                continue;
            }

            var rewardType = config.RewardType?.Trim().ToLowerInvariant();
            var rewardBucket = NormalizeRewardBucket(config.Status);

            switch (rewardType)
            {
                case "item":
                    AddItemReward(
                        config.QuestId,
                        request.ItemId,
                        config.Count,
                        config.FindInRaid,
                        false,
                        rewardBucket);
                    break;

                case "ammo":
                    AddAmmoReward(
                        config.QuestId,
                        request.ItemId,
                        config.Count,
                        config.FindInRaid,
                        rewardBucket);
                    break;

                case "weapon":
                    AddWeaponReward(
                        config.QuestId,
                        request.ItemId,
                        config.FindInRaid,
                        rewardBucket);
                    break;

                case "weaponpreset":
                    if (string.IsNullOrWhiteSpace(config.PresetId))
                    {
                        _debugLogHelper.LogError("QuestReward", $"Missing presetId for quest {config.QuestId}");
                        continue;
                    }

                    AddWeaponPresetReward(
                        config.QuestId,
                        config.PresetId,
                        config.FindInRaid,
                        config.IsHidden,
                        rewardBucket);
                    break;

                case "currency":
                    AddCurrencyReward(
                        config.QuestId,
                        config.CurrencyTpl ?? RubTpl,
                        config.Count,
                        rewardBucket);
                    break;

                default:
                    _debugLogHelper.LogError(
                        "QuestReward",
                        $"Unknown rewardType '{config.RewardType}' for item {request.ItemId}");
                    break;
            }
        }
    }

    public void AddItemReward(
        string questId,
        string itemTpl,
        int count,
        bool findInRaid = false,
        bool unknown = false,
        string rewardBucket = SuccessBucket)
    {
        if (!TryGetQuest(questId, out var quest))
        {
            return;
        }

        EnsureRewardBuckets(quest);

        var normalizedBucket = NormalizeRewardBucket(rewardBucket);
        var rewards = quest.Rewards![normalizedBucket];

        var rewardRootId = new MongoId();
        var rewardItemId = new MongoId();

        var reward = new Reward
        {
            Id = rewardRootId,
            Type = RewardType.Item,
            Index = rewards.Count,
            FindInRaid = findInRaid,
            Unknown = unknown,
            Value = count,
            Target = rewardItemId.ToString(),
            IsHidden = false,
            GameMode = ["regular", "pve"],
            AvailableInGameEditions = [],
            Items =
            [
                new Item
                {
                    Id = rewardItemId,
                    Template = itemTpl
                }
            ]
        };

        rewards.Add(reward);

        _debugLogHelper.LogService(
            "QuestReward",
            $"Added item reward {itemTpl} x{count} to quest {questId} ({normalizedBucket})");
    }

    public void AddAmmoReward(
        string questId,
        string ammoTpl,
        int count,
        bool findInRaid = false,
        string rewardBucket = SuccessBucket)
    {
        AddItemReward(questId, ammoTpl, count, findInRaid, false, rewardBucket);
    }

    public void AddCurrencyReward(
        string questId,
        string currencyTpl,
        int amount,
        string rewardBucket = SuccessBucket)
    {
        var tpl = NormalizeCurrencyTpl(currencyTpl);
        AddItemReward(questId, tpl, amount, false, false, rewardBucket);
    }

    public void AddWeaponReward(
        string questId,
        string weaponTpl,
        bool findInRaid = false,
        string rewardBucket = SuccessBucket)
    {
        AddItemReward(questId, weaponTpl, 1, findInRaid, false, rewardBucket);
    }

    public void AddWeaponPresetReward(
        string questId,
        string presetId,
        bool findInRaid = false,
        bool isHidden = false,
        string rewardBucket = SuccessBucket)
    {
        if (!TryGetQuest(questId, out var quest))
        {
            return;
        }

        EnsureRewardBuckets(quest);

        var builtPreset = _builtPresetCache.GetByPresetId(presetId);
        if (builtPreset == null)
        {
            _debugLogHelper.LogError(
                "QuestReward",
                $"Built preset template for source preset {presetId} not found in cache for quest {questId}");
            return;
        }

        if (builtPreset.Items == null || builtPreset.Items.Count == 0)
        {
            _debugLogHelper.LogError(
                "QuestReward",
                $"Built preset template for source preset {presetId} has no items");
            return;
        }

        var rootSourceId = _builtPresetCache.ResolveFinalOfferIdFromPresetId(presetId);
        if (string.IsNullOrWhiteSpace(rootSourceId))
        {
            _debugLogHelper.LogError(
                "QuestReward",
                $"Built preset template for source preset {presetId} has invalid RootBuiltItemId");
            return;
        }

        var idMap = new Dictionary<string, MongoId>(StringComparer.OrdinalIgnoreCase);

        foreach (var sourceItem in builtPreset.Items)
        {
            idMap[sourceItem.Id.ToString()] = new MongoId();
        }

        var rewardItems = new List<Item>();

        foreach (var sourceItem in builtPreset.Items)
        {
            var sourceId = sourceItem.Id.ToString();
            var newId = idMap[sourceId];

            string? newParentId = null;
            var sourceParentId = sourceItem.ParentId?.ToString();

            if (!string.IsNullOrWhiteSpace(sourceParentId)
                && !string.Equals(sourceParentId, "hideout", StringComparison.OrdinalIgnoreCase))
            {
                if (!idMap.TryGetValue(sourceParentId, out var mappedParent))
                {
                    _debugLogHelper.LogError(
                        "QuestReward",
                        $"Failed to map parent {sourceParentId} for preset reward clone item {sourceId} from source preset {presetId}");
                    return;
                }

                newParentId = mappedParent.ToString();
            }

            var newItem = new Item
            {
                Id = newId,
                Template = sourceItem.Template,
                ParentId = newParentId,
                SlotId = sourceItem.SlotId,
                Location = sourceItem.Location,
                Upd = CloneUpd(sourceItem.Upd)
            };

            if (string.Equals(sourceId, rootSourceId, StringComparison.OrdinalIgnoreCase))
            {
                newItem.ParentId = null;
                newItem.SlotId = null;
            }

            if (findInRaid)
            {
                newItem.Upd ??= new Upd();
                newItem.Upd.SpawnedInSession = true;
            }

            rewardItems.Add(newItem);

            _debugLogHelper.LogService(
                "QuestReward",
                $"Preset reward clone from source preset {presetId}: {sourceId} -> {newItem.Id}, parent={newItem.ParentId}, tpl={newItem.Template}");
        }

        if (!idMap.TryGetValue(rootSourceId, out var rewardRootId))
        {
            _debugLogHelper.LogError(
                "QuestReward",
                $"Could not resolve reward root for source preset {presetId}");
            return;
        }

        var normalizedBucket = NormalizeRewardBucket(rewardBucket);

        var reward = new Reward
        {
            Id = new MongoId(),
            Type = RewardType.Item,
            Index = quest.Rewards![normalizedBucket].Count,
            Target = rewardRootId.ToString(),
            Value = 1,
            FindInRaid = findInRaid,
            IsHidden = isHidden,
            Unknown = false,
            GameMode = ["regular", "pve"],
            AvailableInGameEditions = [],
            Items = rewardItems
        };

        quest.Rewards![normalizedBucket].Add(reward);

        _debugLogHelper.LogService(
            "QuestReward",
            $"Added preset reward using source preset reference {presetId} to quest {questId} with {rewardItems.Count} items ({normalizedBucket})");
    }

    private bool TryGetQuest(string questId, out Quest quest)
    {
        quest = null!;

        if (string.IsNullOrWhiteSpace(questId))
        {
            _debugLogHelper.LogError("QuestReward", "questId is null or empty");
            return false;
        }

        if (!_databaseService.GetTables().Templates.Quests.TryGetValue(questId, out quest))
        {
            _debugLogHelper.LogError("QuestReward", $"Quest {questId} not found");
            return false;
        }

        return true;
    }

    private static void EnsureRewardBuckets(Quest quest)
    {
        quest.Rewards ??= new Dictionary<string, List<Reward>>(StringComparer.OrdinalIgnoreCase);

        if (!quest.Rewards.ContainsKey(StartedBucket))
        {
            quest.Rewards[StartedBucket] = [];
        }

        if (!quest.Rewards.ContainsKey(SuccessBucket))
        {
            quest.Rewards[SuccessBucket] = [];
        }

        if (!quest.Rewards.ContainsKey(FailBucket))
        {
            quest.Rewards[FailBucket] = [];
        }
    }

    private string NormalizeCurrencyTpl(string currencyTpl)
    {
        if (currencyTpl.Equals("RUB", StringComparison.OrdinalIgnoreCase))
        {
            return RubTpl;
        }

        if (currencyTpl.Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            return UsdTpl;
        }

        if (currencyTpl.Equals("EUR", StringComparison.OrdinalIgnoreCase))
        {
            return EurTpl;
        }

        if (currencyTpl != RubTpl && currencyTpl != UsdTpl && currencyTpl != EurTpl)
        {
            _debugLogHelper.LogError("QuestReward", $"Invalid currency tpl {currencyTpl}, defaulting to RUB");
            return RubTpl;
        }

        return currencyTpl;
    }

    private static string NormalizeRewardBucket(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return SuccessBucket;
        }

        if (status.Equals(StartedBucket, StringComparison.OrdinalIgnoreCase))
        {
            return StartedBucket;
        }

        if (status.Equals(FailBucket, StringComparison.OrdinalIgnoreCase))
        {
            return FailBucket;
        }

        return SuccessBucket;
    }

    private static Upd? CloneUpd(Upd? original)
    {
        if (original == null)
        {
            return null;
        }

        return new Upd
        {
            UnlimitedCount = original.UnlimitedCount,
            StackObjectsCount = original.StackObjectsCount,
            BuyRestrictionMax = original.BuyRestrictionMax,
            BuyRestrictionCurrent = original.BuyRestrictionCurrent,
            Repairable = original.Repairable,
            Foldable = original.Foldable,
            FireMode = original.FireMode,
            Key = original.Key,
            MedKit = original.MedKit,
            Resource = original.Resource,
            Dogtag = original.Dogtag,
            FoodDrink = original.FoodDrink,
            RecodableComponent = original.RecodableComponent,
            RepairKit = original.RepairKit,
            Togglable = original.Togglable,
            FaceShield = original.FaceShield,
            Sight = original.Sight,
            SpawnedInSession = original.SpawnedInSession
        };
    }
}