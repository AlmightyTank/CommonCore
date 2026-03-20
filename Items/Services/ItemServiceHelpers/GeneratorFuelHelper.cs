using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable]
public sealed class GeneratorFuelHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db)
{
    private const string GeneratorAreaId = "5d3b396e33c48f02b81cd9f3";

    public void Process(ItemCreationRequest request)
    {
        var generator = db.Hideout.Areas.Find(a => a.Id == GeneratorAreaId);
        var validStages = request.GeneratorFuelSlotStages;

        if (generator == null)
        {
            debugLogHelper.LogError("GeneratorFuelHelper", $"Generator not found in hideout areas.");
            return;
        }

        if (validStages == null || validStages.Length == 0)
        {
            return;
        }

        if (generator.Stages == null)
        {
            debugLogHelper.LogError("GeneratorFuelHelper", $"Generator has no stages.");
            return;
        }

        foreach (var validStage in validStages)
        {
            if (!generator.Stages.TryGetValue(validStage.ToString(), out Stage? stage) || stage == null)
            {
                debugLogHelper.LogError("GeneratorFuelHelper", $"Stage {validStage} not found in generator fuel.");
                continue;
            }

            if (stage.Bonuses == null)
            {
                continue;
            }

            foreach (var bonus in stage.Bonuses)
            {
                if (bonus is not { Type: BonusType.AdditionalSlots, Filter: { } filter })
                {
                    continue;
                }

                if (filter.Contains(request.NewId))
                {
                    continue;
                }

                filter.Add(request.NewId);
                debugLogHelper.LogService("GeneratorFuelHelper", $"Added item {request.NewId} as fuel to generator at stage with bonus ID {bonus.Id}");
            }
        }
    }
}