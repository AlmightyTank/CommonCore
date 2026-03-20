using CommonCore.Core;
using CommonCore.Constants;
using CommonCore.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Utils;
using CommonCore.Items.Models;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public sealed class StaticLootHelper(
    CommonCoreDb db,
    CoreDebugLogHelper debugLogHelper)
{
    private readonly CommonCoreDb _db = db;

    public void Process(ItemCreationRequest request)
    {
        if (!request.AddToStaticLootContainers)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.NewId))
        {
            debugLogHelper.LogError("StaticLootHelper", $"NewId missing.");
            return;
        }

        if (request.StaticLootContainers == null || request.StaticLootContainers.Length == 0)
        {
            debugLogHelper.LogError("StaticLootHelper", $"No StaticLootContainers provided for {request.NewId}.");
            return;
        }

        var itemTpl = new MongoId(request.NewId);
        AddItemToStaticContainers(itemTpl, request.StaticLootContainers);
    }

    public void AddItemToStaticContainers(MongoId itemTpl, StaticLootContainerEntry[] containers)
    {
        if (containers == null || containers.Length == 0)
        {
            return;
        }

        foreach (var container in containers)
        {
            if (container == null || string.IsNullOrWhiteSpace(container.ContainerName))
            {
                continue;
            }

            AddToStaticLoot(container, itemTpl);
        }
    }

    private void AddToStaticLoot(StaticLootContainerEntry containerEntry, MongoId itemTpl)
    {
        if (_db.Locations.Count == 0)
        {
            debugLogHelper.LogError("StaticLootHelper", $"Locations dictionary was empty.");
            return;
        }

        foreach (var (locationId, location) in _db.Locations)
        {
            if (location.StaticLoot is null)
            {
                continue;
            }

            location.StaticLoot.AddTransformer(staticLootData =>
            {
                if (staticLootData is null)
                {
                    return staticLootData;
                }

                var resolvedContainerIds = ResolveContainerIds(containerEntry.ContainerName, staticLootData.Keys);
                if (resolvedContainerIds.Count == 0)
                {
                    LogHelper.LogDebug($"[StaticLoot] Could not resolve container '{containerEntry.ContainerName}' in {locationId}.");
                    return staticLootData;
                }

                foreach (var containerId in resolvedContainerIds)
                {
                    if (!staticLootData.TryGetValue(containerId, out var containerDetails))
                    {
                        continue;
                    }

                    ApplyDistribution(containerDetails, itemTpl, containerEntry, locationId, containerId);
                }

                return staticLootData;
            });
        }
    }

    private void ApplyDistribution(
        StaticLootDetails? containerDetails,
        MongoId itemTpl,
        StaticLootContainerEntry entry,
        string locationId,
        MongoId containerId)
    {
        if (containerDetails is null)
        {
            debugLogHelper.LogError("StaticLootHelper", $"Container '{containerId}' in {locationId} is null.");
            return;
        }

        var itemDistribution = containerDetails.ItemDistribution?.ToList() ?? [];

        var existing = itemDistribution.FirstOrDefault(x => x.Tpl == itemTpl);
        if (existing != null)
        {
            if (entry.ReplaceProbabilityIfExists)
            {
                existing.RelativeProbability = entry.Probability;
                containerDetails.ItemDistribution = itemDistribution.ToArray();

                LogHelper.LogDebug(
                    $"[StaticLoot] Updated {itemTpl} in {containerId} ({locationId}) to probability {entry.Probability}.");
            }
            else
            {
                LogHelper.LogDebug(
                    $"[StaticLoot] {itemTpl} already exists in {containerId} ({locationId}), skipping.");
            }

            return;
        }

        itemDistribution.Add(new ItemDistribution
        {
            Tpl = itemTpl,
            RelativeProbability = entry.Probability
        });

        containerDetails.ItemDistribution = itemDistribution.ToArray();

        debugLogHelper.LogService("StaticLootHelper", $"Added {itemTpl} to {containerId} ({locationId}) with probability {entry.Probability}.");
    }

    private List<MongoId> ResolveContainerIds(string containerNameOrId, IEnumerable<MongoId> availableContainerIds)
    {
        var resolved = new List<MongoId>();

        // 1. Friendly alias map
        if (ItemMaps.ContainerMap.TryGetValue(containerNameOrId, out var mappedId))
        {
            resolved.Add(mappedId);
            return resolved;
        }

        // 2. Direct MongoId
        try
        {
            var directId = ItemTplResolver.ResolveId(containerNameOrId);
            if (directId.IsValidMongoId())
            {
                resolved.Add(directId);
                return resolved;
            }
        }
        catch
        {
            // ignored, try wildcard/grouping below
        }

        // 3. Wildcard/all support
        if (containerNameOrId == "*" || containerNameOrId.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            resolved.AddRange(availableContainerIds);
            return resolved;
        }

        // 4. Group aliases like "weapon", "medical", etc.
        if (ItemMaps.ContainerGroups.TryGetValue(containerNameOrId, out var groupIds))
        {
            resolved.AddRange(groupIds.Where(id => availableContainerIds.Contains(id)));
            return resolved.Distinct().ToList();
        }

        return resolved;
    }
}