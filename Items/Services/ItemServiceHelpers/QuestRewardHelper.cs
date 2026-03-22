using CommonCore.Core;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public sealed class QuestRewardHelper(
    ISptLogger<QuestRewardHelper> logger,
    CommonCoreDb db)
{
    private const string StartedBucket = "Started";
    private const string SuccessBucket = "Success";
    private const string FailBucket = "Fail";

    private const string RubTpl = "5449016a4bdc2d6f028b456f";
    private const string UsdTpl = "5696686a4bdc2da3298b456a";
    private const string EurTpl = "569668774bdc2da2298b4568";

    public void Process(ItemCreationRequest request)
    {
        if (request.QuestRewards == null || request.QuestRewards.Length == 0)
        {
            return;
        }

        foreach (var config in request.QuestRewards)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.QuestId))
            {
                logger.Warning($"[QuestReward] Missing questId for item {request.NewId}");
                continue;
            }

            switch (config.RewardType?.Trim())
            {
                case "Item":
                    AddItemReward(config.QuestId, request.NewId, config.Count, config.FindInRaid, config.IsHidden);
                    break;

                case "Ammo":
                    AddAmmoReward(config.QuestId, request.NewId, config.Count, config.FindInRaid, config.IsHidden);
                    break;

                case "Weapon":
                    AddWeaponReward(config.QuestId, request.NewId, config.FindInRaid, config.IsHidden);
                    break;

                case "WeaponPreset":
                    if (string.IsNullOrWhiteSpace(config.PresetId))
                    {
                        logger.Warning($"[QuestReward] Missing presetId for quest {config.QuestId}");
                        continue;
                    }

                    AddWeaponPresetReward(config.QuestId, config.PresetId, config.FindInRaid, config.IsHidden);
                    break;

                case "Currency":
                    AddCurrencyReward(config.QuestId, config.Count, config.CurrencyTpl ?? RubTpl, config.IsHidden);
                    break;

                default:
                    logger.Warning($"[QuestReward] Unknown rewardType '{config.RewardType}' for item {request.NewId}");
                    break;
            }
        }
    }

    public void AddItemReward(
        string questId,
        string itemTpl,
        int count = 1,
        bool findInRaid = false,
        bool isHidden = false)
    {
        if (!TryGetQuest(questId, out var quest))
        {
            return;
        }

        if (!db.Items.ContainsKey(itemTpl))
        {
            logger.Warning($"[QuestReward] Item tpl {itemTpl} not found, skipping reward for quest {questId}");
            return;
        }

        EnsureRewardBuckets(quest);

        var rewardId = new MongoId();
        var itemId = new MongoId();

        var reward = new Reward
        {
            Id = rewardId,
            Type = RewardType.Item,
            Target = itemId.ToString(),
            Value = count,
            FindInRaid = findInRaid,
            IsHidden = isHidden,
            IsEncoded = false,
            Unknown = false,
            GameMode = ["regular", "pve"],
            AvailableInGameEditions = [],
            Items =
            [
                new Item
                {
                    Id = itemId,
                    Template = itemTpl,
                    Upd = new Upd
                    {
                        StackObjectsCount = count,
                        SpawnedInSession = findInRaid
                    }
                }
            ]
        };

        quest.Rewards![SuccessBucket].Add(reward);
        db.Quests[questId].Rewards["Success"].Add(reward);
        logger.Debug($"[QuestReward] Added item reward {itemTpl} x{count} to quest {questId}");
    }

    public void AddAmmoReward(
        string questId,
        string ammoTpl,
        int count,
        bool findInRaid = false,
        bool isHidden = false)
    {
        AddItemReward(questId, ammoTpl, count, findInRaid, isHidden);
    }

    public void AddCurrencyReward(
        string questId,
        int amount,
        string currencyTpl = RubTpl,
        bool isHidden = false)
    {
        if (!TryGetQuest(questId, out var quest))
        {
            return;
        }

        EnsureRewardBuckets(quest);

        var normalizedTpl = NormalizeCurrencyTpl(currencyTpl);
        var rewardId = new MongoId();
        var itemId = new MongoId();

        var reward = new Reward
        {
            Id = rewardId,
            Type = RewardType.Item,
            Target = itemId.ToString(),
            Value = amount,
            FindInRaid = false,
            IsHidden = isHidden,
            IsEncoded = false,
            Unknown = false,
            GameMode = ["regular", "pve"],
            AvailableInGameEditions = [],
            Items =
            [
                new Item
                {
                    Id = itemId,
                    Template = normalizedTpl,
                    Upd = new Upd
                    {
                        StackObjectsCount = amount
                    }
                }
            ]
        };

        quest.Rewards![SuccessBucket].Add(reward);
        db.Quests[questId].Rewards["Success"].Add(reward);
        logger.Debug($"[QuestReward] Added currency reward {normalizedTpl} x{amount} to quest {questId}");
    }

    public void AddWeaponReward(
        string questId,
        string weaponTpl,
        bool findInRaid = false,
        bool isHidden = false)
    {
        AddItemReward(questId, weaponTpl, 1, findInRaid, isHidden);
    }

    public void AddWeaponPresetReward(
        string questId,
        string presetId,
        bool findInRaid = false,
        bool isHidden = false)
    {
        if (!TryGetQuest(questId, out var quest))
        {
            return;
        }

        if (!db.Presets.TryGetValue(presetId, out var preset) || preset == null)
        {
            logger.Warning($"[QuestReward] Preset {presetId} not found, skipping reward for quest {questId}");
            return;
        }

        if (preset.Items == null || preset.Items.Count == 0)
        {
            logger.Warning($"[QuestReward] Preset {presetId} has no items, skipping reward for quest {questId}");
            return;
        }

        EnsureRewardBuckets(quest);

        var idMap = new Dictionary<string, MongoId>(StringComparer.OrdinalIgnoreCase);
        foreach (var presetItem in preset.Items)
        {
            idMap[presetItem.Id.ToString()] = new MongoId();
        }

        var rootPresetItem = preset.Items[0];
        var rewardTargetId = idMap[rootPresetItem.Id.ToString()];
        var rewardItems = new List<Item>();

        foreach (var presetItem in preset.Items)
        {
            var newId = idMap[presetItem.Id.ToString()];

            string? newParentId = null;
            if (!string.IsNullOrWhiteSpace(presetItem.ParentId) &&
                idMap.TryGetValue(presetItem.ParentId, out var mappedParentId))
            {
                newParentId = mappedParentId;
            }

            var newItem = new Item
            {
                Id = newId,
                Template = presetItem.Template,
                ParentId = newParentId,
                SlotId = presetItem.SlotId,
                Upd = CloneUpd(presetItem.Upd)
            };

            if (newId == rewardTargetId)
            {
                newItem.Upd ??= new Upd();
                newItem.Upd.SpawnedInSession = findInRaid;
            }

            rewardItems.Add(newItem);
        }

        var reward = new Reward
        {
            Id = new MongoId(),
            Type = RewardType.Item,
            Target = rewardTargetId.ToString(),
            Value = 1,
            FindInRaid = findInRaid,
            IsHidden = isHidden,
            IsEncoded = false,
            Unknown = false,
            GameMode = ["regular", "pve"],
            AvailableInGameEditions = [],
            Items = rewardItems
        };

        quest.Rewards![SuccessBucket].Add(reward);
        db.Quests[questId].Rewards["Success"].Add(reward);
        logger.Debug($"[QuestReward] Added weapon preset reward {presetId} to quest {questId}");
    }

    private bool TryGetQuest(string questId, out Quest quest)
    {
        quest = null!;

        if (string.IsNullOrWhiteSpace(questId))
        {
            logger.Warning("[QuestReward] questId is null or empty");
            return false;
        }

        if (!db.Quests.TryGetValue(questId, out quest))
        {
            logger.Warning($"[QuestReward] Quest {questId} not found");
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
            logger.Warning($"[QuestReward] Invalid currency tpl {currencyTpl}, defaulting to RUB");
            return RubTpl;
        }

        return currencyTpl;
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