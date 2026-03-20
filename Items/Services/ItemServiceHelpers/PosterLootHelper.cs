using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable]
public class PosterLootHelper(CommonCoreDb db, CoreDebugLogHelper debugLogHelper)
{
    public void Process(ItemCreationRequest request)
    {
        if (request.AddPosterToMaps != true)
            return;

        var itemId = request.NewId;
        var locations = db.Locations;

        foreach (var (locationId, location) in locations)
        {
            if (location.LooseLoot is null)
                continue;

            location.LooseLoot.AddTransformer(lazyLoadedLooseLootData =>
            {
                foreach (var spawnpoint in lazyLoadedLooseLootData?.Spawnpoints ?? [])
                {
                    var template = spawnpoint.Template;
                    if (template is null)
                        continue;

                    var templateId = template.Id;
                    if (string.IsNullOrEmpty(templateId) ||
                        !templateId.StartsWith("flyer", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var spawnPointItems = new List<SptLootItem>(template.Items ?? []);
                    if (spawnPointItems.Any(it => it.Template == itemId))
                        continue;

                    var itemDistList = new List<LooseLootItemDistribution>(spawnpoint.ItemDistribution ?? []);
                    var newId = new MongoId();

                    spawnPointItems.Add(new SptLootItem
                    {
                        Id = newId,
                        Template = itemId,
                        ComposedKey = newId,
                        Upd = new Upd { StackObjectsCount = 1 }
                    });

                    itemDistList.Add(new LooseLootItemDistribution
                    {
                        ComposedKey = new ComposedKey { Key = newId },
                        RelativeProbability = request.PosterSpawnProbability
                    });
                        debugLogHelper.LogService("PosterLootHelper",
                            $"[PosterLoot] {locationId} + {spawnpoint.LocationId ?? "?"} id={templateId} key={newId}");

                    template.Items = spawnPointItems;
                    spawnpoint.ItemDistribution = itemDistList;
                }

                return lazyLoadedLooseLootData;
            });
        }
    }
}