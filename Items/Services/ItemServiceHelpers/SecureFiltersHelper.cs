using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable]
public sealed class SecureFiltersHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db)
{
    private const string SecureContainerParentId = "5448bf274bdc2dfc2f8b456a";
    private const string BossContainerId = "5c0a794586f77461c458f892";

    public void Process(ItemCreationRequest request)
    {
        if (request.AddToSecureFilters != true)
        {
            return;
        }

        foreach (var (_, itemTemplate) in db.Items)
        {
            if (itemTemplate.Parent != SecureContainerParentId)
            {
                continue;
            }

            if (itemTemplate.Id == BossContainerId)
            {
                continue;
            }

            var grids = itemTemplate.Properties?.Grids?.ToList();
            if (grids == null || grids.Count == 0)
            {
                continue;
            }

            var gridFilters = grids[0].Properties?.Filters?.FirstOrDefault();
            if (gridFilters?.Filter == null)
            {
                debugLogHelper.LogError("SecureFiltersHelper",
                    $"Failed to add {request.NewId} to secure container {itemTemplate.Id} filters (filters don't exist). " +
                    "Check your SVM settings or load this mod before conflicting mods.");
                continue;
            }

            if (!gridFilters.Filter.Contains(request.NewId))
            {
                gridFilters.Filter.Add(request.NewId);
                debugLogHelper.LogService("SecureFiltersHelper", $"Added {request.NewId} to secure container {itemTemplate.Id}");
            }
        }
    }
}