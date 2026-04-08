using CommonLibExtended.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Services;
using System.Globalization;

namespace CommonLibExtended.Items.Services.ItemHelpers;

[Injectable]
public sealed class SpawnCloneHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService)
{
    private static readonly HashSet<string> SupportedMaps = new(StringComparer.OrdinalIgnoreCase)
    {
        "bigmap",
        "woods",
        "factory4_day",
        "factory4_night",
        "interchange",
        "laboratory",
        "lighthouse",
        "rezervbase",
        "shoreline",
        "tarkovstreets",
        "sandbox"
    };

    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly DatabaseService _databaseService = databaseService;

    public void AddSpawnsInSamePlacesAsOrigin(string newItemId, string originItemId, double? spawnWeightComparedToOrigin)
    {
        if (string.IsNullOrWhiteSpace(newItemId))
        {
            _debugLogHelper.LogError("SpawnCloneHelper", "newItemId was null or empty");
            return;
        }

        if (string.IsNullOrWhiteSpace(originItemId))
        {
            _debugLogHelper.LogError("SpawnCloneHelper", "originItemId was null or empty");
            return;
        }

        var weightMultiplier = spawnWeightComparedToOrigin ?? 1d;
        if (weightMultiplier < 0)
        {
            _debugLogHelper.LogError("SpawnCloneHelper", $"spawnWeightComparedToOrigin must be >= 0, got {weightMultiplier.ToString(CultureInfo.InvariantCulture)}");
            return;
        }

        var locations = _databaseService.GetLocations();
        if (locations == null || locations.GetDictionary().Count == 0)
        {
            _debugLogHelper.LogError("SpawnCloneHelper", "Locations table was null or empty");
            return;
        }

        var looseLootAdds = 0;
        var staticLootAdds = 0;
        var looseLootComposedKey = $"{newItemId}_composedkey";

        foreach (var (mapName, mapData) in locations.GetDictionary())
        {
            if (!SupportedMaps.Contains(mapName) || mapData == null)
            {
                continue;
            }

            looseLootAdds += AddLooseLootSpawns(mapName, mapData, originItemId, newItemId, looseLootComposedKey, weightMultiplier);
            staticLootAdds += AddStaticLootSpawns(mapName, mapData, originItemId, newItemId, weightMultiplier);
        }

        _debugLogHelper.LogService(
            "SpawnCloneHelper",
            $"Added {looseLootAdds} loose loot spawn entries and {staticLootAdds} static loot entries for {newItemId} cloned from {originItemId}");
    }

    private int AddLooseLootSpawns(
        string mapName,
        dynamic mapData,
        string originItemId,
        string newItemId,
        string composedKey,
        double weightMultiplier)
    {
        var added = 0;

        var spawnPoints = mapData?.LooseLoot?.Spawnpoints ?? mapData?.looseLoot?.spawnpoints;
        if (spawnPoints == null)
        {
            return 0;
        }

        foreach (var point in spawnPoints)
        {
            if (point?.Template?.Items == null || point?.ItemDistribution == null)
            {
                continue;
            }

            var alreadyExists = false;
            foreach (var existingItem in point.Template.Items)
            {
                if (existingItem?._tpl == newItemId)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (alreadyExists)
            {
                continue;
            }

            foreach (var itm in point.Template.Items)
            {
                if (itm?._tpl != originItemId)
                {
                    continue;
                }

                var originalItemKey = itm._id?.ToString();
                if (string.IsNullOrWhiteSpace(originalItemKey))
                {
                    continue;
                }

                double? originRelativeProb = null;

                foreach (var dist in point.ItemDistribution)
                {
                    var distKey = dist?.ComposedKey?.Key?.ToString() ?? dist?.composedKey?.key?.ToString();
                    if (string.Equals(distKey, originalItemKey, StringComparison.OrdinalIgnoreCase))
                    {
                        originRelativeProb = (double?)dist.RelativeProbability ?? (double?)dist.relativeProbability;
                        break;
                    }
                }

                if (originRelativeProb == null)
                {
                    continue;
                }

                point.Template.Items.Add(new
                {
                    _id = composedKey,
                    _tpl = newItemId
                });

                point.ItemDistribution.Add(new
                {
                    composedKey = new
                    {
                        key = composedKey
                    },
                    relativeProbability = Math.Max((int)Math.Round(originRelativeProb.Value * weightMultiplier), 1)
                });

                added++;
                break;
            }
        }

        if (added > 0)
        {
            _debugLogHelper.LogService("SpawnCloneHelper", $"Added {added} loose loot spawn entries on {mapName} for {newItemId}");
        }

        return added;
    }

    private int AddStaticLootSpawns(
        string mapName,
        dynamic mapData,
        string originItemId,
        string newItemId,
        double weightMultiplier)
    {
        var added = 0;

        var staticLoot = mapData?.StaticLoot ?? mapData?.staticLoot;
        if (staticLoot == null)
        {
            return 0;
        }

        foreach (var containerEntry in staticLoot)
        {
            var container = containerEntry.Value;
            if (container?.ItemDistribution == null && container?.itemDistribution == null)
            {
                continue;
            }

            var itemDistribution = container.ItemDistribution ?? container.itemDistribution;

            var alreadyExists = false;
            foreach (var entry in itemDistribution)
            {
                var tpl = entry?.Tpl?.ToString() ?? entry?.tpl?.ToString();
                if (string.Equals(tpl, newItemId, StringComparison.OrdinalIgnoreCase))
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (alreadyExists)
            {
                continue;
            }

            object? originEntry = null;
            foreach (var entry in itemDistribution)
            {
                var tpl = entry?.Tpl?.ToString() ?? entry?.tpl?.ToString();
                if (string.Equals(tpl, originItemId, StringComparison.OrdinalIgnoreCase))
                {
                    originEntry = entry;
                    break;
                }
            }

            if (originEntry == null)
            {
                continue;
            }

            var originProbability =
                (double?)originEntry.GetType().GetProperty("RelativeProbability")?.GetValue(originEntry) ??
                (double?)originEntry.GetType().GetProperty("relativeProbability")?.GetValue(originEntry);

            if (originProbability == null)
            {
                continue;
            }

            itemDistribution.Add(new
            {
                tpl = newItemId,
                relativeProbability = Math.Max((int)Math.Round(originProbability.Value * weightMultiplier), 1)
            });

            added++;
        }

        if (added > 0)
        {
            _debugLogHelper.LogService("SpawnCloneHelper", $"Added {added} static loot entries on {mapName} for {newItemId}");
        }

        return added;
    }
}