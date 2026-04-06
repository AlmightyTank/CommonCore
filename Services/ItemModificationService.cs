using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using CommonLibExtended.Services.ItemHelpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Services;

namespace CommonLibExtended.Services;

[Injectable]
public sealed class ItemModificationService(
    DebugLogHelper debugLogHelper,
    QuestAssortHelper questAssortHelper,
    CompatibilityService compatibilityService,
    SlotCloneHelper slotCloneHelper,
    PresetTraderOfferHelper presetTraderOfferHelper,
    QuestRewardHelper questRewardHelper,
    EquipmentSlotHelper equipmentSlotHelper,
    CompatibilityCloneHelper compatibilityCloneHelper,
    SpawnCloneHelper spawnCloneHelper,
    QuestConditionCloneHelper questConditionCloneHelper,
    QuestHelper questHelper,
    DatabaseService databaseService)
{
    public void ProcessCloneCompatibilities(IEnumerable<ItemModificationRequest> requests)
    {
        foreach (var request in requests)
        {
            if (!ValidateRequest(request))
            {
                continue;
            }

            if ((request.Extras?.AmmoCloneCompatibility == true)
                || (request.Extras?.WeaponCloneChamberCompatibility == true)
                || (request.Extras?.MagCloneCartridgeCompatibility == true))
            {
                compatibilityCloneHelper.Process(request);
                debugLogHelper.LogService("CompatibilityCloneHelper", $"Added clone compatibility for {request.ItemId}");
            }

            if (request.Extras?.AmmoCloneCompatibility == true &&
                !string.IsNullOrWhiteSpace(request.Config.ItemTplToClone))
            {
                compatibilityService.AddAmmoClone(request.ItemId, request.Config.ItemTplToClone);
                debugLogHelper.LogService("CompatibilityService",
                    $"Added ammo clone compatibility for {request.ItemId} based on {request.Config.ItemTplToClone}");
            }
        }
    }

    public void ProcessQuestConditions(IEnumerable<ItemModificationRequest> requests)
    {
        var quests = databaseService.GetQuests();

        foreach (var request in requests)
        {
            if (!ValidateRequest(request))
            {
                continue;
            }

            if (request.Extras?.AddToQuestConditions != true ||
                request.Extras.QuestConditions == null ||
                request.Extras.QuestConditions.Count == 0)
            {
                continue;
            }

            foreach (var questCondition in request.Extras.QuestConditions)
            {
                if (questCondition == null || string.IsNullOrWhiteSpace(questCondition.QuestId))
                {
                    debugLogHelper.LogError(
                        nameof(ItemModificationService),
                        $"Quest condition config was null or missing questId for {request.ItemId}");
                    continue;
                }

                switch (questCondition.Type)
                {
                    case "AddWeaponsToKillCondition":
                        questHelper.AddWeaponsToKillCondition(
                            quests,
                            questCondition.QuestId,
                            [request.ItemId]);

                        debugLogHelper.LogService(
                            "QuestHelper",
                            $"Added {request.ItemId} to kill conditions for quest {questCondition.QuestId}");
                        break;

                    case "AddWeaponsToFindOrHandoverCondition":
                        questHelper.AddWeaponsToFindOrHandoverCondition(
                            quests,
                            questCondition.QuestId,
                            [request.ItemId]);

                        debugLogHelper.LogService(
                            "QuestHelper",
                            $"Added {request.ItemId} to find/handover conditions for quest {questCondition.QuestId}");
                        break;

                    case "AddArmorToEquipmentExclusive":
                        questHelper.AddArmorToEquipmentExclusive(
                            quests,
                            questCondition.QuestId,
                            [request.ItemId]);

                        debugLogHelper.LogService(
                            "QuestHelper",
                            $"Added {request.ItemId} to equipment exclusive conditions for quest {questCondition.QuestId}");
                        break;

                    case "AddWeaponModToCondition":
                        if (string.IsNullOrWhiteSpace(questCondition.ExistingModId))
                        {
                            debugLogHelper.LogError(
                                "QuestHelper",
                                $"existingModId is required for AddWeaponModToCondition on item {request.ItemId}");
                            break;
                        }

                        questHelper.AddWeaponModToCondition(
                            quests,
                            questCondition.QuestId,
                            request.ItemId,
                            questCondition.ExistingModId,
                            questCondition.IsInclusive ?? true);

                        debugLogHelper.LogService(
                            "QuestHelper",
                            $"Added mod {request.ItemId} to weapon mod conditions for quest {questCondition.QuestId}");
                        break;

                    case "AddDogtagsToQuests":
                        if (string.IsNullOrWhiteSpace(questCondition.Faction))
                        {
                            debugLogHelper.LogError(
                                "QuestHelper",
                                $"faction is required for AddDogtagsToQuests on item {request.ItemId}");
                            break;
                        }

                        questHelper.AddDogtagsToQuests(
                            quests,
                            questCondition.QuestId,
                            [request.ItemId],
                            questCondition.Faction);

                        debugLogHelper.LogService(
                            "QuestHelper",
                            $"Added dogtag {request.ItemId} to quest {questCondition.QuestId} for faction {questCondition.Faction}");
                        break;

                    default:
                        debugLogHelper.LogError(
                            "QuestHelper",
                            $"Unknown quest condition type '{questCondition.Type}' for item {request.ItemId}");
                        break;
                }
            }
        }
    }

    public void ProcessQuestConditionClones(IEnumerable<ItemModificationRequest> requests)
    {
        foreach (var request in requests)
        {
            if (!ValidateRequest(request))
            {
                continue;
            }

            if (request.Extras.IncludeInSameQuestsAsOrigin == true
                && !string.IsNullOrWhiteSpace(request.Config.ItemTplToClone))
            {
                questConditionCloneHelper.AddToSameQuestsAsOrigin(
                    request.ItemId,
                    request.Config.ItemTplToClone);

                debugLogHelper.LogService(
                    "QuestConditionCloneHelper",
                    $"Added {request.ItemId} to same quest conditions as {request.Config.ItemTplToClone}");
            }
        }
    }

    public void ProcessSpawnClones(IEnumerable<ItemModificationRequest> requests)
    {
        foreach (var request in requests)
        {
            if (!ValidateRequest(request))
            {
                continue;
            }

            if (request.Extras.AddSpawnsInSamePlacesAsOrigin == true
                && !string.IsNullOrWhiteSpace(request.Config.ItemTplToClone))
            {
                spawnCloneHelper.AddSpawnsInSamePlacesAsOrigin(
                    request.ItemId,
                    request.Config.ItemTplToClone,
                    request.Extras.SpawnWeightComparedToOrigin);

                debugLogHelper.LogService(
                    "SpawnCloneHelper",
                    $"Added spawn clone entries for {request.ItemId} based on {request.Config.ItemTplToClone}");
            }
        }
    }

    public void ProcessSlotCopies(IEnumerable<ItemModificationRequest> requests)
    {
        foreach (var request in requests)
        {
            if (!ValidateRequest(request))
            {
                continue;
            }

            if (request.Extras?.CopySlot == true)
            {
                slotCloneHelper.Process(request);
                debugLogHelper.LogService("SlotCloneHelper", $"Copied slots for {request.ItemId}");
            }
        }
    }

    public void ProcessPresetTraders(IEnumerable<ItemModificationRequest> requests)
    {
        foreach (var request in requests)
        {
            if (!ValidateRequest(request))
            {
                continue;
            }

            if (request.Extras?.PresetTraders is { Count: > 0 })
            {
                presetTraderOfferHelper.Process(request);
                debugLogHelper.LogService("PresetTraderOfferHelper", $"Added preset traders for {request.ItemId}");
            }
        }
    }

    public void ProcessEquipmentSlots(IEnumerable<ItemModificationRequest> requests)
    {
        foreach (var request in requests)
        {
            if (!ValidateRequest(request))
            {
                continue;
            }

            if (request.Extras?.AddToPrimaryWeaponSlot == true ||
                request.Extras?.AddToHolsterWeaponSlot == true)
            {
                equipmentSlotHelper.Process(request);
                debugLogHelper.LogService("EquipmentSlotHelper", $"Added {request.ItemId} to equipment slots");
            }
        }
    }

    public void ProcessQuestAssorts(IEnumerable<ItemModificationRequest> requests)
    {
        foreach (var request in requests)
        {
            if (!ValidateRequest(request))
            {
                continue;
            }

            if (request.Extras?.AddToQuestAssorts == true && request.Extras.QuestAssorts != null)
            {
                questAssortHelper.Process(request);
                debugLogHelper.LogService("QuestAssortHelper", $"Added quest assorts for {request.ItemId}");
            }
        }
    }

    public void ProcessQuestRewards(IEnumerable<ItemModificationRequest> requests)
    {
        foreach (var request in requests)
        {
            if (!ValidateRequest(request))
            {
                continue;
            }

            if (request.Extras?.AddToQuestRewards == true && request.Extras?.QuestRewards is { Count: > 0 })
            {
                questRewardHelper.Process(request);
                debugLogHelper.LogService("QuestRewardHelper", $"Added quest rewards for {request.ItemId}");
            }
        }
    }

    private bool ValidateRequest(ItemModificationRequest request)
    {
        if (request == null)
        {
            debugLogHelper.Log(nameof(ItemModificationService), "Received null request");
            return false;
        }

        if (request.Config == null)
        {
            debugLogHelper.LogError(nameof(ItemModificationService), $"Request config is null for {request.ItemId}");
            return false;
        }

        if (request.Extras == null)
        {
            debugLogHelper.LogError(nameof(ItemModificationService), $"Request extras are null for {request.ItemId}");
            return false;
        }

        try
        {
            request.Config.Validate(request.ItemId);
            request.Extras.Validate(request.ItemId);
        }
        catch (Exception ex)
        {
            debugLogHelper.LogError(nameof(ItemModificationService),
                $"Validation failed for {request.ItemId} from {request.FilePath}: {ex.Message}");
            return false;
        }

        return true;
    }
}