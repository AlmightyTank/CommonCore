using CommonCore.Items.Services;
using CommonCore.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonCore.Helpers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public class CraftHelper(
    CoreDebugLogHelper debugLogHelper,
    DatabaseService databaseService
)
{
    private readonly DatabaseService _databaseService = databaseService;

    public void Process(CommonCoreItemRequest request)
    {
        if (!request.Config.AddCrafts == true)
        {
            return;
        }

        if (request.Config.Crafts == null || request.Config.Crafts.Length == 0)
        {
            debugLogHelper.LogError("CraftHelper", $"Invalid crafts for {request.ItemId}");
            return;
        }

        AddCrafts(request.Config.Crafts);
        debugLogHelper.LogService("CraftHelper", $"Added {request.Config.Crafts.Length} crafts for {request.ItemId}");
    }

    public void AddCrafts(HideoutProduction[] crafts)
    {
        foreach (var craft in crafts)
        {
            if (craft == null)
            {
                continue;
            }

            if (_databaseService.GetHideout().Production.Recipes.Any(x => string.Equals(x.Id, craft.Id, StringComparison.OrdinalIgnoreCase)))
            {
                debugLogHelper.LogService("CraftHelper", $"Craft {craft.Id} already exists, skipping.");
                continue;
            }

            _databaseService.GetHideout().Production.Recipes.Add(craft);
        }
    }
}