using CommonCore.Helpers;
using CommonCore.ItemServiceHelpers;
using CommonCore.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Services;
using AssortHelper = CommonCore.ItemServiceHelpers.AssortHelper;
using QuestRewardHelper = CommonCore.ItemServiceHelpers.QuestRewardHelper;

namespace CommonCore.Items.Services;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public class ItemFeaturePipeline(
    CoreDebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    SlotCloneHelper slotCloneHelper,
    CompatibilityCloneHelper compatibilityCloneHelper,
    BuffHelper buffHelper,
    CraftHelper craftHelper,
    AssortHelper assortHelper,
    ScriptedConflictHelper scriptedConflictHelper,
    EquipmentSlotHelper equipmentSlotHelper,
    QuestRewardHelper questRewardHelper,
    QuestAssortHelper questAssortHelper
)
{
    public Task ProcessItemFeatures(CommonCoreItemRequest request)
    {
        debugLogHelper.LogService(
            "CommonCoreItemService",
            $"Processing features for {request.ItemId}... - {request.Config.AddToQuestAssorts}"
        );

        if (databaseService == null)
        {
            return Task.CompletedTask;
        }

        if (request.Config.CopySlot == true)
            slotCloneHelper.Process(request);

        if (request.Config.AmmoCloneCompatibility == true ||
            request.Config.WeaponCloneChamberCompatibility == true ||
            request.Config.MagCloneCartridgeCompatibility == true)
        {
            compatibilityCloneHelper.Process(request);
        }

        if (request.Config.AddBuffs == true)
            buffHelper.Process(request);

        if (request.Config.AddToPrimaryWeaponSlot == true || request.Config.AddToHolsterWeaponSlot == true)
            equipmentSlotHelper.Process(request);

        if (request.Config.ScriptedConflictingInfos != null && request.Config.ScriptedConflictingInfos.Length > 0)
            scriptedConflictHelper.Process(request);

        if (request.Config.AddCrafts == true)
            craftHelper.Process(request);

        if (request.Config.AdditionalAssortData != null)
            assortHelper.Process(request);

        if (request.Config.AddToQuestAssorts == true)
            questAssortHelper.Process(request);

        if (request.Config.AddToQuestRewards == true)
            questRewardHelper.Process(request);

        return Task.CompletedTask;
    }
}