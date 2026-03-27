using CommonCore.Core;
using CommonCore.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using static CommonCore.Models.CommonCoreItemConfig;

namespace CommonCore.Helpers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public sealed class QuestRewardHelper(
    CoreDebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    BuiltPresetCache builtPresetCache)
{
    private const string StartedBucket = "Started";
    private const string SuccessBucket = "Success";
    private const string FailBucket = "Fail";

    private const string RubTpl = "5449016a4bdc2d6f028b456f";
    private const string UsdTpl = "5696686a4bdc2da3298b456a";
    private const string EurTpl = "569668774bdc2da2298b4568";

    public void Process(CommonCoreItemRequest request)
    {
        if (request.Config.QuestRewards == null || request.Config.QuestRewards.Count == 0)
        {
            return;
        }

        foreach (var config in request.Config.QuestRewards)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.QuestId))
            {
                debugLogHelper.LogError("QuestReward", $"Missing questId for item {request.ItemId}");
                continue;
            }

            var rewardType = config.RewardType?.Trim();

            switch (rewardType)
            {
                case "Item":
                    AddItemReward(
                        config.QuestId,
                        request.ItemId,
                        config.Count,
                        config.FindInRaid,
                        false,
                        NormalizeRewardBucket(config.Status));
                    break;

                case "Ammo":
                    AddAmmoReward(
                        config.QuestId,
                        request.ItemId,
                        config.Count,
                        config.FindInRaid,
                        NormalizeRewardBucket(config.Status));
                    break;

                case "Weapon":
                    AddWeaponReward(
                        config.QuestId,
                        request.ItemId,
                        config.FindInRaid,
                        NormalizeRewardBucket(config.Status));
                    break;

                case "WeaponPreset":
                    if (string.IsNullOrWhiteSpace(config.PresetId))
                    {
                        debugLogHelper.LogError("QuestReward", $"Missing presetId for quest {config.QuestId}");
                        continue;
                    }

                    AddWeaponPresetReward(
                        config.QuestId,
                        config.PresetId,
                        config.FindInRaid,
                        config.IsHidden,
                        NormalizeRewardBucket(config.Status));
                    break;

                case "Currency":
                    AddCurrencyReward(
                        config.QuestId,
                        config.CurrencyTpl ?? RubTpl,
                        config.Count,
                        NormalizeRewardBucket(config.Status));
                    break;

                default:
                    debugLogHelper.LogError(
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

        var rewards = quest.Rewards![rewardBucket];

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

        debugLogHelper.LogService(
            "QuestReward",
            $"Added item reward {itemTpl} x{count} to {questId} ({rewardBucket})");
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

        var builtPreset = builtPresetCache.GetByPresetId(presetId);
        if (builtPreset == null)
        {
            debugLogHelper.LogError(
                "QuestReward",
                $"Built preset {presetId} not found in cache for quest {questId}");

            return;
        }

        var idMap = new Dictionary<string, MongoId>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in builtPreset.Items)
        {
            idMap[item.Id.ToString()] = new MongoId();
        }

        var rewardItems = new List<Item>();

        foreach (var sourceItem in builtPreset.Items)
        {
            var oldId = sourceItem.Id.ToString();
            var newId = idMap[oldId];

            string? newParentId = null;
            if (!string.IsNullOrWhiteSpace(sourceItem.ParentId)
                && !string.Equals(sourceItem.ParentId, "hideout", StringComparison.OrdinalIgnoreCase)
                && idMap.TryGetValue(sourceItem.ParentId, out var mappedParent))
            {
                newParentId = mappedParent.ToString();
            }

            var newItem = new Item
            {
                Id = newId,
                Template = sourceItem.Template,
                ParentId = newParentId,
                SlotId = sourceItem.SlotId,
                Upd = CloneUpd(sourceItem.Upd)
            };

            if (oldId == builtPreset.RootBuiltItemId)
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

            debugLogHelper.LogService(
                "QuestReward",
                $"Reward clone: {oldId} -> {newItem.Id}, parent={newItem.ParentId}, tpl={newItem.Template}");
        }

        var rootItem = rewardItems.FirstOrDefault(x => x.ParentId == null) ?? rewardItems.First();

        var reward = new Reward
        {
            Id = new MongoId(),
            Type = RewardType.Item,
            Index = quest.Rewards![rewardBucket].Count,
            Target = rootItem.Id.ToString(),
            Value = 1,
            FindInRaid = findInRaid,
            IsHidden = isHidden,
            Unknown = false,
            GameMode = ["regular", "pve"],
            AvailableInGameEditions = [],
            Items = rewardItems
        };

        quest.Rewards![rewardBucket].Add(reward);

        debugLogHelper.LogService(
            "QuestReward",
            $"Added BUILT preset reward {presetId} -> quest {questId} with {rewardItems.Count} items ({rewardBucket})");
    }

    private bool TryGetQuest(string questId, out Quest quest)
    {
        quest = null!;

        if (string.IsNullOrWhiteSpace(questId))
        {
            debugLogHelper.LogError("QuestReward", "questId is null or empty");
            return false;
        }

        if (!databaseService.GetTables().Templates.Quests.TryGetValue(questId, out quest))
        {
            debugLogHelper.LogError("QuestReward", $"Quest {questId} not found");
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
            debugLogHelper.LogError("QuestReward", $"Invalid currency tpl {currencyTpl}, defaulting to RUB");
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