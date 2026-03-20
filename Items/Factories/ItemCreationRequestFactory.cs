using CommonCore.Items.Models;

namespace CommonCore.Items.Factories;

public static class ItemCreationRequestFactory
{
    public static ItemCreationRequest FromConfig(string itemId, ItemCreationRequest config)
    {
        var request = new ItemCreationRequest
        {
            NewId = itemId,

            ItemTplToClone = config.ItemTplToClone,
            ParentId = config.ParentId,
            HandbookParentId = config.HandbookParentId,

            FleaPriceRoubles = config.FleaPriceRoubles,
            HandbookPriceRoubles = config.HandbookPriceRoubles,

            Locales = config.Locales,
            OverrideProperties = config.OverrideProperties,

            AddToTraders = config.AddToTraders,
            Traders = config.Traders,
            TraderId = config.TraderId,
            TraderLoyaltyLevel = config.TraderLoyaltyLevel,
            BarterSchemes = config.BarterSchemes,
            BuyRestrictionMax = config.BuyRestrictionMax,

            AddPresetInsteadOfItem = config.AddPresetInsteadOfItem,
            PresetIdToAdd = config.PresetIdToAdd,

            AddWeaponPreset = config.AddWeaponPreset,
            AddToPreset = config.AddToPreset,
            Presets = config.Presets,

            Masteries = config.Masteries,
            AddMasteries = config.AddMasteries,
            WeaponCloneMasteriesId = config.WeaponCloneMasteriesId,

            AddToModSlots = config.AddToModSlots,
            AddtoModSlots = config.AddtoModSlots,
            AddtoModSlotsCloneId = config.AddtoModSlotsCloneId,
            ModSlot = config.ModSlot,
            AddtoConflicts = config.AddtoConflicts,

            CopySlot = config.CopySlot,
            CopySlotsInfo = config.CopySlotsInfo,

            AddSlot = config.AddSlot,
            SlotsToAdd = config.SlotsToAdd,

            AddToInventorySlots = config.AddToInventorySlots,

            AddToHallOfFame = config.AddToHallOfFame,
            HallOfFameSlots = config.HallOfFameSlots,
            AddToSpecialSlots = config.AddToSpecialSlots,

            AddToStaticLootContainers = config.AddToStaticLootContainers,
            StaticLootContainers = config.StaticLootContainers,

            AddToBots = config.AddToBots,

            AddCaliberToAllCloneLocations = config.AddCaliberToAllCloneLocations,

            AddToGeneratorAsFuel = config.AddToGeneratorAsFuel,
            GeneratorFuelSlotStages = config.GeneratorFuelSlotStages,

            AddToHideoutPosterSlots = config.AddToHideoutPosterSlots,

            AddPosterToMaps = config.AddPosterToMaps,
            PosterSpawnProbability = config.PosterSpawnProbability,

            AddToStatuetteSlots = config.AddToStatuetteSlots,

            AddToStaticAmmo = config.AddToStaticAmmo,
            StaticAmmoProbability = config.StaticAmmoProbability,

            AddToEmptyPropSlots = config.AddToEmptyPropSlots,
            EmptyPropSlot = config.EmptyPropSlot,

            AddToSecureFilters = config.AddToSecureFilters,

            IsRandomLootContainer = config.IsRandomLootContainer,
            RandomLootContainerRewards = config.RandomLootContainerRewards,

            AmmoCloneCompatibility = config.AmmoCloneCompatibility,
            WeaponCloneChamberCompatibility = config.WeaponCloneChamberCompatibility,
            WeaponCloneChamberId = config.WeaponCloneChamberId,

            MagCloneCartridgeCompatibility = config.MagCloneCartridgeCompatibility,
            MagCloneCartridgeId = config.MagCloneCartridgeId,

            AddBuffs = config.AddBuffs,
            Buffs = config.Buffs,

            AddCrafts = config.AddCrafts,
            Crafts = config.Crafts,

            AdditionalAssortData = config.AdditionalAssortData,
            ScriptedConflictingInfos = config.ScriptedConflictingInfos,

            AddToPrimaryWeaponSlot = config.AddToPrimaryWeaponSlot,
            AddToHolsterWeaponSlot = config.AddToHolsterWeaponSlot
        };

        request.Normalize();
        return request;
    }
}