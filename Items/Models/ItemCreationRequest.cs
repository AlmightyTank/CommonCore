using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace CommonCore.Items.Models;

public class ItemCreationRequest
{
    [JsonPropertyName("newId")]
    public string NewId { get; set; } = string.Empty;

    [JsonPropertyName("itemTplToClone")]
    public string? ItemTplToClone { get; set; }

    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }

    [JsonPropertyName("handbookParentId")]
    public string? HandbookParentId { get; set; }

    [JsonPropertyName("fleaPriceRoubles")]
    public int? FleaPriceRoubles { get; set; }

    [JsonPropertyName("handbookPriceRoubles")]
    public int? HandbookPriceRoubles { get; set; }

    [JsonPropertyName("locales")]
    public Dictionary<string, Dictionary<string, string>>? Locales { get; set; }

    [JsonPropertyName("overrideProperties")]
    public TemplateItemProperties? OverrideProperties { get; set; }

    [JsonPropertyName("addToTraders")]
    public bool AddToTraders { get; set; }

    [JsonPropertyName("traders")]
    public Dictionary<string, Dictionary<string, ConfigTraderScheme>>? Traders { get; set; }

    [JsonPropertyName("traderId")]
    public string? TraderId { get; set; }

    [JsonPropertyName("traderLoyaltyLevel")]
    public int? TraderLoyaltyLevel { get; set; }

    [JsonPropertyName("barterSchemes")]
    public BarterScheme[]? BarterSchemes { get; set; }

    [JsonPropertyName("buyRestrictionMax")]
    public int? BuyRestrictionMax { get; set; }

    [JsonPropertyName("addPresetInsteadOfItem")]
    public bool AddPresetInsteadOfItem { get; set; }

    [JsonPropertyName("presetIdToAdd")]
    public string? PresetIdToAdd { get; set; }

    [JsonPropertyName("addWeaponPreset")]
    public bool AddWeaponPreset { get; set; }

    [JsonPropertyName("addToPreset")]
    public bool AddToPreset { get; set; }

    [JsonPropertyName("presets")]
    public Preset[]? Presets { get; set; }

    [JsonPropertyName("masteries")]
    public IEnumerable<Mastering>? Masteries { get; set; }

    [JsonPropertyName("addMasteries")]
    public bool AddMasteries { get; set; }

    [JsonPropertyName("weaponCloneMasteriesId")]
    public string? WeaponCloneMasteriesId { get; set; }

    [JsonPropertyName("addToModSlots")]
    public bool AddToModSlots { get; set; }

    // support legacy typo
    [JsonPropertyName("addtoModSlots")]
    public bool AddtoModSlots { get; set; }

    [JsonPropertyName("addtoModSlotsCloneId")]
    public string? AddtoModSlotsCloneId { get; set; }

    [JsonPropertyName("modSlot")]
    public string[]? ModSlot { get; set; }

    [JsonPropertyName("addtoConflicts")]
    public bool AddtoConflicts { get; set; }

    [JsonPropertyName("copySlot")]
    public bool CopySlot { get; set; }

    [JsonPropertyName("copySlotsInfo")]
    public CopySlotInfo[]? CopySlotsInfo { get; set; }

    [JsonPropertyName("addSlot")]
    public bool AddSlot { get; set; }

    [JsonPropertyName("slotsToAdd")]
    public Slot[]? SlotsToAdd { get; set; }

    [JsonPropertyName("addToInventorySlots")]
    public List<string>? AddToInventorySlots { get; set; }

    [JsonPropertyName("addToHallOfFame")]
    public bool AddToHallOfFame { get; set; }

    [JsonPropertyName("hallOfFameSlots")]
    public List<string>? HallOfFameSlots { get; set; }

    [JsonPropertyName("addToSpecialSlots")]
    public bool AddToSpecialSlots { get; set; }

    [JsonPropertyName("addToStaticLootContainers")]
    public bool AddToStaticLootContainers { get; set; }

    [JsonPropertyName("staticLootContainers")]
    public StaticLootContainerEntry[]? StaticLootContainers { get; set; }

    [JsonPropertyName("addToBots")]
    public bool AddToBots { get; set; }

    [JsonPropertyName("addCaliberToAllCloneLocations")]
    public bool AddCaliberToAllCloneLocations { get; set; }

    [JsonPropertyName("addToGeneratorAsFuel")]
    public bool AddToGeneratorAsFuel { get; set; }

    [JsonPropertyName("generatorFuelSlotStages")]
    public int[]? GeneratorFuelSlotStages { get; set; }

    [JsonPropertyName("addToHideoutPosterSlots")]
    public bool AddToHideoutPosterSlots { get; set; }

    [JsonPropertyName("addPosterToMaps")]
    public bool AddPosterToMaps { get; set; }

    [JsonPropertyName("posterSpawnProbability")]
    public float? PosterSpawnProbability { get; set; }

    [JsonPropertyName("addToStatuetteSlots")]
    public bool AddToStatuetteSlots { get; set; }

    [JsonPropertyName("addToStaticAmmo")]
    public bool AddToStaticAmmo { get; set; }

    [JsonPropertyName("staticAmmoProbability")]
    public int? StaticAmmoProbability { get; set; }

    [JsonPropertyName("addToEmptyPropSlots")]
    public bool AddToEmptyPropSlots { get; set; }

    [JsonPropertyName("emptyPropSlot")]
    public EmptyPropSlotConfig? EmptyPropSlot { get; set; }

    [JsonPropertyName("addToSecureFilters")]
    public bool AddToSecureFilters { get; set; }

    [JsonPropertyName("isRandomLootContainer")]
    public bool IsRandomLootContainer { get; set; }

    [JsonPropertyName("randomLootContainerRewards")]
    public RewardDetails? RandomLootContainerRewards { get; set; }

    [JsonPropertyName("ammoCloneCompatibility")]
    public bool AmmoCloneCompatibility { get; set; }

    [JsonPropertyName("weaponCloneChamberCompatibility")]
    public bool WeaponCloneChamberCompatibility { get; set; }

    [JsonPropertyName("weaponCloneChamberId")]
    public string? WeaponCloneChamberId { get; set; }

    [JsonPropertyName("magCloneCartridgeCompatibility")]
    public bool MagCloneCartridgeCompatibility { get; set; }

    [JsonPropertyName("magCloneCartridgeId")]
    public string? MagCloneCartridgeId { get; set; }

    [JsonPropertyName("addBuffs")]
    public bool AddBuffs { get; set; }

    [JsonPropertyName("buffs")]
    public Dictionary<string, Buff[]>? Buffs { get; set; }

    [JsonPropertyName("addCrafts")]
    public bool AddCrafts { get; set; }

    [JsonPropertyName("crafts")]
    public HideoutProduction[]? Crafts { get; set; }

    [JsonPropertyName("additionalAssortData")]
    public TraderAssort? AdditionalAssortData { get; set; }

    [JsonPropertyName("scriptedConflictingInfos")]
    public ConflictingInfos[]? ScriptedConflictingInfos { get; set; }

    [JsonPropertyName("addToPrimaryWeaponSlot")]
    public bool AddToPrimaryWeaponSlot { get; set; }

    [JsonPropertyName("addToHolsterWeaponSlot")]
    public bool AddToHolsterWeaponSlot { get; set; }

    public void Normalize()
    {
        AddToPreset = AddToPreset || AddWeaponPreset;
        AddToModSlots = AddToModSlots || AddtoModSlots;
        AddMasteries = AddMasteries || Masteries != null;

        WeaponCloneMasteriesId ??= ItemTplToClone;
        WeaponCloneChamberId ??= ItemTplToClone;
        MagCloneCartridgeId ??= ItemTplToClone;
        AddtoModSlotsCloneId ??= ItemTplToClone;
    }
}