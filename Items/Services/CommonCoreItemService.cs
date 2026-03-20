using CommonCore.Constants;
using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Factories;
using CommonCore.Items.Models;
using CommonCore.Items.Services.ItemServiceHelpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Mod;
using System.Reflection;
using AssortHelper = CommonCore.Items.Services.ItemServiceHelpers.AssortHelper;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;
using Path = System.IO.Path;

namespace CommonCore.Items.Services;

[Injectable(InjectionType.Singleton)]
public class CommonCoreItemService(
    CoreDebugLogHelper debugLogHelper,
    CustomItemService customItemService,
    CommonCoreDb db,
    ModHelper modHelper,
    ConfigHelper configHelper,
    LoadedItemRegistry loadedItemRegistry,
    TraderItemHelper traderItemHelper,
    WeaponPresetHelper weaponPresetHelper,
    StaticLootHelper staticLootHelper,
    SpecialSlotsHelper specialSlotsHelper,
    PosterLootHelper posterLootHelper,
    ModSlotHelper modSlotHelper,
    MasteryHelper masteryHelper,
    InventorySlotHelper inventorySlotHelper,
    HideoutStatuetteHelper hideoutStatuetteHelper,
    HideoutPosterHelper hideoutPosterHelper,
    HallOfFameHelper hallOfFameHelper,
    GeneratorFuelHelper generatorFuelHelper,
    CaliberHelper caliberHelper,
    BotLootHelper botLootHelper,
    StaticAmmoHelper staticAmmoHelper,
    EmptyPropSlotHelper emptyPropSlotHelper,
    SecureFiltersHelper secureFiltersHelper,
    RandomLootContainerHelper randomLootContainerHelper,
    SlotCloneHelper slotCloneHelper,
    SlotAddHelper slotAddHelper,
    CompatibilityCloneHelper compatibilityCloneHelper,
    BuffHelper buffHelper,
    CraftHelper craftHelper,
    AssortHelper assortHelper,
    ScriptedConflictHelper scriptedConflictHelper,
    LocaleHelper localeHelper,
    EquipmentSlotHelper equipmentSlotHelper
)
{
    private readonly List<ItemCreationRequest> _deferredModSlotRequests = [];
    private readonly List<ItemCreationRequest> _deferredSecureFilterRequests = [];
    private readonly List<ItemCreationRequest> _deferredCaliberRequests = [];

    public async Task CreateCustomItems(Assembly assembly, string? relativePath = null)
    {
        try
        {
            var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            var defaultDir = Path.Combine("db", "CustomItems");
            var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

            await CreateCustomItemsFromDirectory(finalDir);
        }
        catch (Exception ex)
        {
            debugLogHelper.LogError("CommonCoreItemService", $"Error loading custom items: {ex.Message}");
        }
    }

    public async Task CreateCustomItemsFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            debugLogHelper.LogError("CommonCoreItemService", $"Directory not found at {directoryPath}");
            return;
        }

        try
        {
            var itemConfigs = await configHelper.LoadAllItemsJsonFiles<ItemCreationRequest>(directoryPath);

            if (itemConfigs.Count == 0)
            {
                debugLogHelper.LogError("CommonCoreItemService", $"No valid item configs found in {directoryPath}");
                return;
            }

            var totalItemsCreated = 0;

            foreach (var config in itemConfigs)
            {
                config.Normalize();

                if (Create(config))
                {
                    totalItemsCreated++;
                }
            }

            debugLogHelper.LogService("CommonCoreItemService", $"Created {totalItemsCreated} custom items from {itemConfigs.Count} files");
        }
        catch (Exception ex)
        {
            debugLogHelper.LogError("CommonCoreItemService", $"Error loading custom items from directory {directoryPath}: {ex.Message}");
        }
    }

    public bool Create(ItemCreationRequest request)
    {
        request.Normalize();

        if (string.IsNullOrWhiteSpace(request.NewId))
        {
            debugLogHelper.LogError("CommonCoreItemService", $"Create: NewId is null or empty.");
            return false;
        }

        if (loadedItemRegistry.Contains(request.NewId))
        {
            debugLogHelper.LogError("CommonCoreItemService", $"Create: Id {request.NewId} duplicated!");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.ItemTplToClone))
        {
            debugLogHelper.LogError("CommonCoreItemService", $"Create: Item {request.NewId} has null ItemTplToClone.");
            return false;
        }

        try
        {
            var cloneId = ItemTplResolver.ResolveId(request.ItemTplToClone);

            if (db?.Templates?.Items?.ContainsKey(cloneId.ToString()!) != true)
            {
                debugLogHelper.LogError("CommonCoreItemService", $"Create: Item {request.NewId} has invalid ItemTplToClone '{request.ItemTplToClone}'.");
                return false;
            }

            var clonedItem = db.Templates.Items[cloneId.ToString()!];

            MongoId parentId;

            if (!string.IsNullOrWhiteSpace(request.ParentId))
            {
                parentId = NameHelper.ResolveId(request.ParentId, ItemMaps.ItemBaseClassMap);
            }
            else
            {
                parentId = new MongoId(clonedItem.Parent);
            }

            var handbookParentId = ResolveHandbookParent(request, cloneId);
            if (handbookParentId == null)
            {
                debugLogHelper.LogError("CommonCoreItemService", $"Create: Failed to resolve handbook parent for {request.NewId}.");
                return false;
            }

            var itemDetails = new NewItemFromCloneDetails
            {
                ItemTplToClone = cloneId,
                ParentId = parentId,
                NewId = request.NewId,
                FleaPriceRoubles = request.FleaPriceRoubles,
                HandbookPriceRoubles = request.HandbookPriceRoubles,
                HandbookParentId = handbookParentId,
                Locales = LocaleConverter.ToLocaleDetails(request.Locales),
                OverrideProperties = request.OverrideProperties
            };

            customItemService.CreateItemFromClone(itemDetails);

            ProcessItemFeatures(request);

            loadedItemRegistry.Add(request.NewId);
            debugLogHelper.LogService("CommonCoreItemService", $"Created item {request.NewId}");

            return true;
        }
        catch (Exception ex)
        {
            debugLogHelper.LogError("CommonCoreItemService", $"Create failed for {request.NewId}: {ex.Message}");
            return false;
        }
    }

    private MongoId? ResolveHandbookParent(ItemCreationRequest request, MongoId cloneId)
    {
        if (!string.IsNullOrWhiteSpace(request.HandbookParentId))
        {
            return NameHelper.ResolveId(request.HandbookParentId, ItemMaps.ItemHandbookCategoryMap);
        }

        return TryGetHandbookParent(cloneId.ToString()!, out var parentId)
            ? parentId
            : null;
    }

    private bool TryGetHandbookParent(string itemTpl, out MongoId parentId)
    {
        parentId = default!;

        try
        {
            var handbook = db?.Templates?.GetType().GetProperty("Handbook")?.GetValue(db.Templates);
            var items = handbook?.GetType().GetProperty("Items")?.GetValue(handbook) as System.Collections.IEnumerable;
            if (items == null)
            {
                return false;
            }

            foreach (var entry in items)
            {
                var id = entry?.GetType().GetProperty("Id")?.GetValue(entry)?.ToString();
                if (!string.Equals(id, itemTpl, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parent = entry?.GetType().GetProperty("ParentId")?.GetValue(entry);
                if (parent is MongoId mongoId)
                {
                    parentId = mongoId;
                    return true;
                }

                return false;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private void ProcessItemFeatures(ItemCreationRequest request)
    {
        if (db == null)
        {
            return;
        }

        if (request.AddToTraders)
            traderItemHelper.Process(request);

        if (request.AddToPreset)
            weaponPresetHelper.Process(request);

        if (request.AddMasteries)
            masteryHelper.Process(request);

        if (request.AddToModSlots)
            AddDeferredModSlotRequest(request);

        if (request.AddToInventorySlots != null)
            inventorySlotHelper.Process(request);

        if (request.AddToHallOfFame)
            hallOfFameHelper.Process(request);

        if (request.AddToSpecialSlots)
            specialSlotsHelper.Process(request);

        if (request.AddToStaticLootContainers)
            staticLootHelper.Process(request);

        if (request.AddToBots)
            botLootHelper.Process(request);

        if (request.AddCaliberToAllCloneLocations)
            AddDeferredCaliberRequest(request);

        if (request.AddToGeneratorAsFuel)
            generatorFuelHelper.Process(request);

        if (request.AddToHideoutPosterSlots)
            hideoutPosterHelper.Process(request);

        if (request.AddPosterToMaps)
            posterLootHelper.Process(request);

        if (request.AddToStatuetteSlots)
            hideoutStatuetteHelper.Process(request);

        if (request.AddToStaticAmmo)
            staticAmmoHelper.Process(request);

        if (request.AddToEmptyPropSlots)
            emptyPropSlotHelper.Process(request);

        if (request.AddToSecureFilters)
            AddDeferredSecureFilterRequest(request);

        if (request.IsRandomLootContainer && request.RandomLootContainerRewards != null)
            randomLootContainerHelper.Process(request);

        if (request.CopySlot)
            slotCloneHelper.Process(request);

        if (request.AddSlot)
            slotAddHelper.Process(request);

        if (request.AmmoCloneCompatibility ||
            request.WeaponCloneChamberCompatibility ||
            request.MagCloneCartridgeCompatibility)
        {
            compatibilityCloneHelper.Process(request);
        }

        if (request.AddBuffs)
            buffHelper.Process(request);

        if (request.AddCrafts)
            craftHelper.Process(request);

        if (request.AdditionalAssortData != null)
            assortHelper.Process(request);

        if (request.ScriptedConflictingInfos != null && request.ScriptedConflictingInfos.Length > 0)
            scriptedConflictHelper.Process(request);

        if (request.Locales != null)
            localeHelper.Process(request);

        if (request.AddToPrimaryWeaponSlot || request.AddToHolsterWeaponSlot)
            equipmentSlotHelper.Process(request);
    }

    private void AddDeferredCaliberRequest(ItemCreationRequest request)
    {
        if (_deferredCaliberRequests.Any(x => x.NewId.Equals(request.NewId, StringComparison.OrdinalIgnoreCase)))
        {
            debugLogHelper.LogError("CommonCoreItemService", $"Deferred caliber request for {request.NewId} already exists, skipping.");
            return;
        }

        _deferredCaliberRequests.Add(request);
    }

    public void ProcessDeferredCalibers()
    {
        if (_deferredCaliberRequests.Count == 0)
        {
            debugLogHelper.LogService("CommonCoreItemService", $"No deferred caliber requests to process");
            return;
        }

        debugLogHelper.LogService("CommonCoreItemService", $"Processing {_deferredCaliberRequests.Count} deferred caliber requests...");

        foreach (var request in _deferredCaliberRequests)
        {
            try
            {
                if (db == null)
                {
                    return;
                }

                caliberHelper.Process(request);
                debugLogHelper.LogService("CommonCoreItemService", $"Processed caliber config for {request.NewId}");
            }
            catch (Exception ex)
            {
                debugLogHelper.LogError("CommonCoreItemService", $"Failed processing caliber config for {request.NewId}");
            }
        }

        _deferredCaliberRequests.Clear();
        debugLogHelper.LogService("CommonCoreItemService", $"Finished processing deferred caliber requests");
    }

    private void AddDeferredModSlotRequest(ItemCreationRequest request)
    {
        if (_deferredModSlotRequests.Any(x => x.NewId.Equals(request.NewId, StringComparison.OrdinalIgnoreCase)))
        {
            debugLogHelper.LogError("CommonCoreItemService", $"Deferred mod slot request for {request.NewId} already exists, skipping.");
            return;
        }

        _deferredModSlotRequests.Add(request);
    }

    public void ProcessDeferredModSlots()
    {
        if (_deferredModSlotRequests.Count == 0)
        {
            debugLogHelper.LogService("CommonCoreItemService", $"No deferred mod slot requests to process");
            return;
        }

        debugLogHelper.LogService("CommonCoreItemService", $"Processing {_deferredModSlotRequests.Count} deferred mod slot requests...");

        foreach (var request in _deferredModSlotRequests)
        {
            try
            {
                if (db == null)
                {
                    return;
                }

                modSlotHelper.Process(request);
                debugLogHelper.LogService("CommonCoreItemService", $"Processed mod slots for {request.NewId}");
            }
            catch (Exception ex)
            {
                debugLogHelper.LogError("CommonCoreItemService", $"Failed processing mod slots for {request.NewId}");
            }
        }

        _deferredModSlotRequests.Clear();
        debugLogHelper.LogService("CommonCoreItemService", $"Finished processing deferred mod slot requests");
    }

    private void AddDeferredSecureFilterRequest(ItemCreationRequest request)
    {
        if (_deferredSecureFilterRequests.Any(x => x.NewId.Equals(request.NewId, StringComparison.OrdinalIgnoreCase)))
        {
            debugLogHelper.LogError("CommonCoreItemService", $"Deferred secure filter request for {request.NewId} already exists, skipping.");
            return;
        }

        _deferredSecureFilterRequests.Add(request);
    }

    public void ProcessDeferredSecureFilters()
    {
        if (_deferredSecureFilterRequests.Count == 0)
        {
            debugLogHelper.LogService("CommonCoreItemService", $"No deferred secure filter requests to process");
            return;
        }

        debugLogHelper.LogService("CommonCoreItemService", $"Processing {_deferredSecureFilterRequests.Count} deferred secure filter requests...");

        foreach (var request in _deferredSecureFilterRequests)
        {
            try
            {
                if (db == null)
                {
                    return;
                }

                secureFiltersHelper.Process(request);
                debugLogHelper.LogService("CommonCoreItemService", $"Processed secure filters for {request.NewId}");
            }
            catch (Exception ex)
            {
                debugLogHelper.LogError("CommonCoreItemService", $"Failed processing secure filters for {request.NewId}");
            }
        }

        _deferredSecureFilterRequests.Clear();
        debugLogHelper.LogService("CommonCoreItemService", $"Finished processing deferred secure filter requests");
    }
}