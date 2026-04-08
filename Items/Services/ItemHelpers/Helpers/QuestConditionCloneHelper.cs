using CommonLibExtended.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Services;

namespace CommonLibExtended.Items.Services.ItemHelpers.Helpers;

[Injectable]
public sealed class QuestConditionCloneHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    CollectionHelper collectionHelper)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly DatabaseService _databaseService = databaseService;

    public void AddToSameQuestsAsOrigin(string newItemId, string originItemId)
    {
        if (string.IsNullOrWhiteSpace(newItemId))
        {
            _debugLogHelper.LogError("QuestConditionCloneHelper", "newItemId was null or empty");
            return;
        }

        if (string.IsNullOrWhiteSpace(originItemId))
        {
            _debugLogHelper.LogError("QuestConditionCloneHelper", "originItemId was null or empty");
            return;
        }

        var quests = _databaseService.GetQuests();
        if (quests == null || quests.Count == 0)
        {
            _debugLogHelper.LogError("QuestConditionCloneHelper", "Quest table was null or empty");
            return;
        }

        var patchedConditions = 0;

        foreach (var (_, quest) in quests)
        {
            var conditions = quest?.Conditions?.AvailableForFinish;
            if (conditions == null || conditions.Count == 0)
            {
                continue;
            }

            foreach (var condition in conditions)
            {
                if (condition == null)
                {
                    continue;
                }

                var conditionType = condition.ConditionType;
                if (!string.Equals(conditionType, "HandoverItem", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(conditionType, "FindItem", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var targets = condition.Target;

                if (!collectionHelper.HasAny(targets))
                {
                    continue;
                }

                if (!collectionHelper.ContainsIgnoreCase(targets, originItemId))
                {
                    continue;
                }

                if (!collectionHelper.AddIfNotExistsIgnoreCase(targets, newItemId))
                {
                    continue;
                }

                patchedConditions++;

                _debugLogHelper.LogService(
                    "QuestConditionCloneHelper",
                    $"Added {newItemId} to quest {quest.Id} finish condition cloned from {originItemId}");
            }
        }

        _debugLogHelper.LogService(
            "QuestConditionCloneHelper",
            $"Patched {patchedConditions} quest finish condition(s) for {newItemId} based on {originItemId}");
    }
}