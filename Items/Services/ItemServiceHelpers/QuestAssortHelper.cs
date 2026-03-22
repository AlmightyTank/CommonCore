using CommonCore.Constants;
using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public sealed class QuestAssortHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db,
    CommonCoreSettings settings)
{
    private const string StartedStatus = "Started";
    private const string SuccessStatus = "Success";
    private const string FailStatus = "Fail";

    public void Process(ItemCreationRequest request)
    {
        if (request.QuestAssorts == null || request.QuestAssorts.Length == 0)
        {
            return;
        }

        var assortId = ResolveAssortId(request);

        foreach (var config in request.QuestAssorts)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.QuestId))
            {
                debugLogHelper.LogError("QuestAssortHelper", $"[QuestAssort] Missing questId for item {request.NewId}");
                continue;
            }

            AddQuestAssort(
                config.TraderId,
                config.QuestId,
                assortId,
                NormalizeStatus(config.Status));
        }
    }

    public void AddQuestAssort(
        string? traderKey,
        string questId,
        MongoId assortId,
        string status = SuccessStatus)
    {
        var resolvedTraderId = ResolveTraderId(traderKey);
        if (string.IsNullOrWhiteSpace(resolvedTraderId))
        {
            debugLogHelper.LogError("QuestAssortHelper", $"[QuestAssort] Could not resolve trader '{traderKey}'");
            return;
        }

        if (!db.Traders.TryGetValue(resolvedTraderId, out var trader))
        {
            debugLogHelper.LogError("QuestAssortHelper", $"[QuestAssort] Trader {resolvedTraderId} not found");
            return;
        }

        if (!questId.IsValidMongoId())
        {
            debugLogHelper.LogError("QuestAssortHelper", $"[QuestAssort] Invalid questId {questId}");
            return;
        }

        var questMongoId = (MongoId)questId;

        if (trader.QuestAssort == null)
        {
            debugLogHelper.LogError("QuestAssortHelper", $"[QuestAssort] Trader {resolvedTraderId} has null QuestAssort and it cannot be reassigned");
            return;
        }

        if (!trader.QuestAssort.TryGetValue(status, out var statusBucket) || statusBucket == null)
        {
            debugLogHelper.LogError("QuestAssortHelper", $"[QuestAssort] Trader {resolvedTraderId} has no QuestAssort bucket '{status}'");
            return;
        }

        if (statusBucket.TryGetValue(questMongoId, out var existingAssortId))
        {
            if (existingAssortId == assortId)
            {
                debugLogHelper.LogService("QuestAssortHelper", $"[QuestAssort] Assort {assortId} already assigned to trader {resolvedTraderId} for quest {questMongoId} ({status})");
                return;
            }

            statusBucket[questMongoId] = assortId;
            db.Traders[resolvedTraderId].QuestAssort[status][questMongoId] = assortId;
            debugLogHelper.LogService("QuestAssortHelper", $"[QuestAssort] Replaced assort {existingAssortId} with {assortId} on trader {resolvedTraderId} for quest {questMongoId} ({status})");
            return;
        }

        statusBucket[questMongoId] = assortId;
        db.Traders[resolvedTraderId].QuestAssort[status][questMongoId] = assortId;
        debugLogHelper.LogService("QuestAssortHelper", $"[QuestAssort] Added assort {assortId} to trader {resolvedTraderId} for quest {questMongoId} ({status})");
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return SuccessStatus;
        }

        if (status.Equals(StartedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return StartedStatus;
        }

        if (status.Equals(FailStatus, StringComparison.OrdinalIgnoreCase))
        {
            return FailStatus;
        }

        return SuccessStatus;
    }

    private MongoId ResolveAssortId(ItemCreationRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.AssortId) && request.AssortId.IsValidMongoId())
        {
            return request.AssortId;
        }

        return GenerateValidAssortId(request.NewId);
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

        if (ItemMaps .TraderMap.TryGetValue(traderKey.ToLower(), out var traderId))
        {
            return traderId;
        }

        if (traderKey.IsValidMongoId())
        {
            return traderKey;
        }

        return null;
    }

    private static MongoId GenerateValidAssortId(string itemId)
    {
        var chars = itemId.ToCharArray();
        chars[0] = chars[0] != '3' ? '3' : '4';
        return new string(chars);
    }
}