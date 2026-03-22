using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using System.Text.Json.Serialization;

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
    public Dictionary<string, ConfigTraderScheme>? Traders { get; set; }

    [JsonPropertyName("assortId")]
    public string? AssortId { get; set; }

    [JsonPropertyName("traderId")]
    public string? TraderId { get; set; }

    [JsonPropertyName("traderLoyaltyLevel")]
    public int? TraderLoyaltyLevel { get; set; }

    [JsonPropertyName("barterSchemes")]
    public BarterScheme[]? BarterSchemes { get; set; }

    [JsonPropertyName("buyRestrictionMax")]
    public int? BuyRestrictionMax { get; set; }
    [JsonPropertyName("questRewards")]
    public QuestRewardConfig[]? QuestRewards { get; set; }
    [JsonPropertyName("addToQuestRewards")]
    public bool AddToQuestRewards { get; set; }
    [JsonPropertyName("questAssorts")]
    public QuestAssortConfig[]? QuestAssorts { get; set; }
    [JsonPropertyName("addToQuestAssorts")]
    public bool AddToQuestAssorts { get; set; }

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
        WeaponCloneMasteriesId ??= ItemTplToClone;
        WeaponCloneChamberId ??= ItemTplToClone;
        MagCloneCartridgeId ??= ItemTplToClone;
        AddtoModSlotsCloneId ??= ItemTplToClone;
    }

    public class QuestRewardConfig
    {
        [JsonPropertyName("questId")]
        public string QuestId { get; set; } = string.Empty;
        [JsonPropertyName("rewardType")]
        public string RewardType { get; set; } = "Item";
        [JsonPropertyName("count")]
        public int Count { get; set; } = 1;
        [JsonPropertyName("findInRaid")]
        public bool FindInRaid { get; set; }
        [JsonPropertyName("isHidden")]
        public bool IsHidden { get; set; }
        [JsonPropertyName("currencyTpl")]
        public string? CurrencyTpl { get; set; }
        [JsonPropertyName("presetId")]
        public string? PresetId { get; set; }
    }

    public class QuestAssortConfig
    {
        [JsonPropertyName("traderId")]
        public string TraderId { get; set; } = string.Empty;
        [JsonPropertyName("questId")]
        public string QuestId { get; set; } = string.Empty;
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Success";
    }

    public class ConflictingInfos
    {
        [JsonPropertyName("id")]
        public MongoId Id { get; set; }
        [JsonPropertyName("tgtSlotName")]
        public required string TgtSlotName { get; set; }
        [JsonPropertyName("itemsAddtoSlot")]
        public string[]? ItemsAddToSlot { get; set; }
    }

    public class EmptyPropSlotConfig
    {
        [JsonPropertyName("itemToAddTo")]
        public string ItemToAddTo { get; set; } = string.Empty;

        [JsonPropertyName("modSlot")]
        public string ModSlot { get; set; } = string.Empty;
    }

    public class CopySlotInfo
    {
        [JsonPropertyName("id")]
        public virtual MongoId Id { get; set; }
        [JsonPropertyName("newSlotName")]
        public required virtual string NewSlotName { get; set; }
        [JsonPropertyName("tgtSlotName")]
        public virtual string? TgtSlotName { get; set; }
        [JsonPropertyName("itemsAddtoSlot")]
        public virtual string[]? ItemsAddToSlot { get; set; }
        [JsonPropertyName("required")]
        public virtual bool? Required { get; set; }
    }

    public class StaticLootContainerEntry
    {
        public string ContainerName { get; set; } = string.Empty;
        public int Probability { get; set; }
        public bool ReplaceProbabilityIfExists { get; set; } = true;
    }

    public class ConfigTraderScheme
    {
        [JsonPropertyName("loyal_level_items")]
        public required ConfigBarterSettings ConfigBarterSettings { get; set; }

        [JsonPropertyName("barter_scheme")]
        public required List<ConfigBarterScheme> Barters { get; set; } = new();
    }

    public class ConfigBarterSettings
    {
        [JsonPropertyName("loyalLevel")]
        public required int LoyalLevel { get; set; }

        [JsonPropertyName("unlimitedCount")]
        public required bool UnlimitedCount { get; set; }

        [JsonPropertyName("stackObjectsCount")]
        public required int StackObjectsCount { get; set; }

        [JsonPropertyName("buyRestrictionMax")]
        public int? BuyRestrictionMax { get; set; }
    }

    public class ConfigBarterScheme
    {
        [JsonPropertyName("count")]
        public virtual double? Count { get; set; }

        [JsonPropertyName("_tpl")]
        public virtual string Template { get; set; } = string.Empty;

        [JsonPropertyName("onlyFunctional")]
        public virtual bool? OnlyFunctional { get; set; }

        [JsonPropertyName("sptQuestLocked")]
        public virtual bool? SptQuestLocked { get; set; }

        [JsonPropertyName("level")]
        public virtual int? Level { get; set; }

        [JsonPropertyName("side")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public virtual DogtagExchangeSide? Side { get; set; }
    }
}