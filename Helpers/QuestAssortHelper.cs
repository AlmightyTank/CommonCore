using CommonCore.Constants;
using CommonCore.Core;
using CommonCore.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonCore.Helpers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public sealed class QuestAssortHelper(
    CoreDebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    CommonCoreSettings settings)
{
    private const string StartedStatus = "started";
    private const string SuccessStatus = "success";
    private const string FailStatus = "fail";

    public void Process(CommonCoreItemRequest request)
    {
        if (request.Config.QuestAssorts == null || request.Config.QuestAssorts.Count == 0)
        {
            return;
        }

        var assortId = ResolveAssortId(request.Config);

        foreach (var config in request.Config.QuestAssorts)
        {
            if (config == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(config.QuestId))
            {
                debugLogHelper.LogError("QuestAssortHelper", $"Missing QuestId for item {request.ItemId}");
                continue;
            }

            var status = NormalizeStatus(config.Status);

            var traderId =
                ResolveTraderIdFromAssort(request.Config, assortId)
                ?? ResolveTraderId(config.TraderId);

            if (string.IsNullOrWhiteSpace(traderId))
            {
                debugLogHelper.LogError("QuestAssortHelper", $"Could not resolve traderId for assort {assortId}");
                continue;
            }

            var success = AddQuestAssort(traderId, config.QuestId, assortId, status);
            if (!success)
            {
                continue;
            }

            var loyaltyLevel = ResolveLoyaltyLevel(request.Config, assortId);

            AddQuestAssortRewardDisplay(
                config.QuestId,
                traderId,
                assortId,
                status,
                request.ItemId,
                loyaltyLevel);
        }
    }

    public bool AddQuestAssort(
        string traderId,
        string questId,
        string assortId,
        string status)
    {
        if (!databaseService.GetTraders().TryGetValue(traderId, out var trader))
        {
            debugLogHelper.LogError("QuestAssortHelper", $"Trader {traderId} not found");
            return false;
        }

        if (trader.QuestAssort == null)
        {
            debugLogHelper.LogError("QuestAssortHelper", $"QuestAssort is null for trader {traderId}");
            return false;
        }

        if (!HasValidTraderAssort(trader, assortId))
        {
            debugLogHelper.LogError("QuestAssortHelper", $"Invalid trader assort {assortId} for trader {traderId}");
            return false;
        }

        if (!trader.QuestAssort.TryGetValue(status, out var bucket))
        {
            debugLogHelper.LogError("QuestAssortHelper", $"Missing quest assort bucket {status} for trader {traderId}");
            return false;
        }

        bucket[assortId] = questId;

        debugLogHelper.LogService(
            "QuestAssortHelper",
            $"Mapped quest assort: trader={traderId}, assort={assortId}, quest={questId}, status={status}");

        return true;
    }

    private void AddQuestAssortRewardDisplay(
        string questId,
        string traderId,
        string assortId,
        string status,
        string template,
        int loyaltyLevel)
    {
        var quests = databaseService.GetTables().Templates.Quests;

        if (!quests.TryGetValue(questId, out var quest))
        {
            debugLogHelper.LogError("QuestAssortHelper", $"Quest {questId} not found");
            return;
        }

        quest.Rewards ??= new Dictionary<string, List<Reward>>(StringComparer.OrdinalIgnoreCase);

        var bucket = NormalizeRewardBucket(status);

        quest.Rewards.TryAdd("Started", []);
        quest.Rewards.TryAdd("Success", []);
        quest.Rewards.TryAdd("Fail", []);

        var rewards = quest.Rewards[bucket];

        if (rewards.Any(x => x.Target == assortId))
        {
            debugLogHelper.LogService(
                "QuestAssortHelper",
                $"Quest assort UI reward already exists for assort {assortId}, quest {questId}");
            return;
        }

        var rewardItems = new List<Item>
        {
            new Item
            {
                Id = assortId,
                Template = template
            }
        };

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

        debugLogHelper.LogService(
            "QuestAssortHelper",
            $"Added quest assort UI reward: quest={questId}, trader={traderId}, assort={assortId}, bucket={bucket}, loyalty={loyaltyLevel}");
    }

    private static bool HasValidTraderAssort(Trader trader, string assortId)
    {
        return trader.Assort?.Items?.Any(x => x.Id == assortId) == true
            && trader.Assort?.BarterScheme?.ContainsKey(assortId) == true
            && trader.Assort?.LoyalLevelItems?.ContainsKey(assortId) == true;
    }

    private static int ResolveLoyaltyLevel(CommonCoreItemConfig request, string assortId)
    {
        if (request.Traders != null)
        {
            foreach (var (_, assortEntries) in request.Traders)
            {
                if (assortEntries != null &&
                    assortEntries.TryGetValue(assortId, out var traderEntry) &&
                    traderEntry?.ConfigBarterSettings != null)
                {
                    return traderEntry.ConfigBarterSettings.LoyalLevel;
                }
            }
        }

        if (request.PresetTraders != null)
        {
            foreach (var (_, assortEntries) in request.PresetTraders)
            {
                if (assortEntries != null &&
                    assortEntries.TryGetValue(assortId, out var presetTraderEntry) &&
                    presetTraderEntry?.ConfigBarterSettings != null)
                {
                    return presetTraderEntry.ConfigBarterSettings.LoyalLevel;
                }
            }
        }

        return 1;
    }

    private string? ResolveTraderIdFromAssort(CommonCoreItemConfig request, string assortId)
    {
        if (request.Traders != null)
        {
            foreach (var (traderKey, assortEntries) in request.Traders)
            {
                if (assortEntries != null && assortEntries.ContainsKey(assortId))
                {
                    return ResolveTraderId(traderKey);
                }
            }
        }

        if (request.PresetTraders != null)
        {
            foreach (var (traderKey, assortEntries) in request.PresetTraders)
            {
                if (assortEntries != null && assortEntries.ContainsKey(assortId))
                {
                    return ResolveTraderId(traderKey);
                }
            }
        }

        return null;
    }

    private static string ResolveAssortId(CommonCoreItemConfig request)
    {
        if (request.Traders != null)
        {
            foreach (var (_, assortEntries) in request.Traders)
            {
                if (assortEntries == null)
                {
                    continue;
                }

                var firstKey = assortEntries.Keys.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstKey) && firstKey.IsValidMongoId())
                {
                    return firstKey;
                }
            }
        }

        if (request.PresetTraders != null)
        {
            foreach (var (_, assortEntries) in request.PresetTraders)
            {
                if (assortEntries == null)
                {
                    continue;
                }

                var firstKey = assortEntries.Keys.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstKey) && firstKey.IsValidMongoId())
                {
                    return firstKey;
                }
            }
        }

        return string.Empty;
    }

    private static string NormalizeRewardBucket(string status)
    {
        if (status.Equals(StartedStatus, StringComparison.OrdinalIgnoreCase)) return "Started";
        if (status.Equals(FailStatus, StringComparison.OrdinalIgnoreCase)) return "Fail";
        return "Success";
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return SuccessStatus;
        return status.ToLowerInvariant();
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