using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable]
public sealed class RandomLootContainerHelper(
    CommonCoreDb db,
    CoreDebugLogHelper debugLogHelper,
    ConfigServer configServer)
{
    public void Process(ItemCreationRequest request)
    {
        var inventoryConfig = configServer.GetConfig<InventoryConfig>();

        var itemInDb = db.Items.GetValueOrDefault(request.NewId);
        if (itemInDb == null)
        {
            debugLogHelper.LogError("RandomLootContainerHelper","Item not found in db. Something is seriously wrong.");
            return;
        }

        if (request.RandomLootContainerRewards == null)
        {
            throw new ArgumentNullException(nameof(request.RandomLootContainerRewards));
        }

        itemInDb.Name = request.NewId;
        inventoryConfig.RandomLootContainers[request.NewId] = request.RandomLootContainerRewards;
    }
}