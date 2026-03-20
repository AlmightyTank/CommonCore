using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public sealed class BotLootHelper(
    CommonCoreDb db,
    CoreDebugLogHelper debugLogHelper)
{
    public void Process(ItemCreationRequest request)
    {
        if (!request.AddToBots)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.NewId))
        {
            debugLogHelper.LogError("BotLootHelper", "Cannot add item to bot loot because NewId is missing.");
            return;
        }

        if (string.IsNullOrWhiteSpace(request.ItemTplToClone))
        {
            debugLogHelper.LogError("BotLootHelper", $"Cannot add {request.NewId} to bot loot because ItemTplToClone is missing.");
            return;
        }

        var cloneItemId = new MongoId(request.ItemTplToClone);
        var newItemId = new MongoId(request.NewId);

        foreach (var (_, bot) in db.Bots.Types)
        {
            var items = bot?.BotInventory?.Items;
            if (items == null)
            {
                continue;
            }

            var containers = new[]
            {
                items.Backpack,
                items.Pockets,
                items.SecuredContainer,
                items.SpecialLoot,
                items.TacticalVest
            };

            foreach (var container in containers)
            {
                if (container == null)
                {
                    continue;
                }

                foreach (var (existingItem, chance) in container.ToList())
                {
                    if (existingItem != cloneItemId)
                    {
                        continue;
                    }

                    if (!container.ContainsKey(newItemId))
                    {
                        container[newItemId] = chance;
                        debugLogHelper.LogService("BotLootHelper", $"Added {request.NewId} to bot loot using clone chance from {request.ItemTplToClone}.");
                    }

                    break;
                }
            }
        }
    }
}