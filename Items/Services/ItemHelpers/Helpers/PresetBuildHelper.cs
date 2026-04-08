using CommonLibExtended.Generator;
using CommonLibExtended.Helpers;
using CommonLibExtended.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;

namespace CommonLibExtended.Items.Services.ItemHelpers.Helpers;

[Injectable]
public sealed class PresetBuildHelper(
    DebugLogHelper debugLogHelper)
{
    public BuiltPresetResult? BuildForTrader(
        Preset preset,
        string rootItemId,
        string logSource = "PresetBuild")
    {
        if (preset.Items == null || preset.Items.Count == 0)
        {
            debugLogHelper.LogError(logSource, $"Preset {preset.Id} has no items");
            return null;
        }

        var rootItem = ResolveRootItem(preset);
        if (rootItem == null)
        {
            debugLogHelper.LogError(logSource, $"Preset {preset.Id} has no root item");
            return null;
        }

        var idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var presetItem in preset.Items)
        {
            var oldId = presetItem.Id.ToString();
            idMap[oldId] = string.Equals(oldId, rootItem.Id.ToString(), StringComparison.OrdinalIgnoreCase)
                ? rootItemId
                : MongoIdGenerator.Generate();
        }

        var builtItems = new List<Item>();

        foreach (var presetItem in preset.Items)
        {
            var oldId = presetItem.Id.ToString();
            var newId = idMap[oldId];
            var isRootItem = string.Equals(oldId, rootItem.Id.ToString(), StringComparison.OrdinalIgnoreCase);

            string? newParentId = null;
            if (presetItem.ParentId != default)
            {
                var oldParentId = presetItem.ParentId.ToString();
                if (idMap.TryGetValue(oldParentId, out var mappedParentId))
                {
                    newParentId = mappedParentId;
                }
            }

            var newItem = new Item
            {
                Id = newId,
                Template = presetItem.Template,
                ParentId = newParentId,
                SlotId = presetItem.SlotId,
                Upd = CloneUpd(presetItem.Upd)
            };

            if (isRootItem)
            {
                newItem.ParentId = "hideout";
                newItem.SlotId = "hideout";
                newItem.Upd ??= new Upd();
                newItem.Upd.UnlimitedCount = true;
                newItem.Upd.StackObjectsCount = 9999999;
            }

            builtItems.Add(newItem);

            debugLogHelper.LogService(
                logSource,
                $"Trader build remap: Preset={preset.Id}, OldId={oldId}, NewId={newItem.Id}, ParentId={newItem.ParentId}, Template={newItem.Template}, SlotId={newItem.SlotId}");
        }

        return new BuiltPresetResult
        {
            SourcePresetId = preset.Id.ToString(),
            RootSourceItemId = rootItem.Id.ToString(),
            RootBuiltItemId = rootItemId,
            Items = builtItems,
            IdMap = idMap
        };
    }

    private static Item? ResolveRootItem(Preset preset)
    {
        return preset.Items.FirstOrDefault(x =>
                   string.Equals(x.Id.ToString(), preset.Parent.ToString(), StringComparison.OrdinalIgnoreCase))
               ?? preset.Items.FirstOrDefault(x => x.ParentId == default)
               ?? preset.Items.FirstOrDefault();
    }

    private static Upd? CloneUpd(Upd? original)
    {
        if (original == null)
        {
            return null;
        }

        return new Upd
        {
            UnlimitedCount = original.UnlimitedCount,
            StackObjectsCount = original.StackObjectsCount,
            BuyRestrictionMax = original.BuyRestrictionMax,
            BuyRestrictionCurrent = original.BuyRestrictionCurrent,
            Repairable = original.Repairable,
            Foldable = original.Foldable,
            FireMode = original.FireMode,
            Key = original.Key,
            MedKit = original.MedKit,
            Resource = original.Resource,
            Dogtag = original.Dogtag,
            FoodDrink = original.FoodDrink,
            RecodableComponent = original.RecodableComponent,
            RepairKit = original.RepairKit,
            Togglable = original.Togglable,
            FaceShield = original.FaceShield,
            Sight = original.Sight,
            SpawnedInSession = original.SpawnedInSession
        };
    }
}