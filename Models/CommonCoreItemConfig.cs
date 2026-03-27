using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using System.Text.Json.Serialization;
using WTTServerCommonLib.Models;

namespace CommonCore.Models;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public class CommonCoreItemConfig : CustomItemConfig
{
    // -------------------------
    // CommonCore extra features
    // -------------------------

    [JsonPropertyName("copySlot")]
    public bool? CopySlot { get; set; }

    [JsonPropertyName("copySlots")]
    public List<CopySlotConfig>? CopySlots { get; set; }

    [JsonPropertyName("ammoCloneCompatibility")]
    public bool? AmmoCloneCompatibility { get; set; }

    [JsonPropertyName("weaponCloneChamberCompatibility")]
    public bool? WeaponCloneChamberCompatibility { get; set; }

    [JsonPropertyName("magCloneCartridgeCompatibility")]
    public bool? MagCloneCartridgeCompatibility { get; set; }

    [JsonPropertyName("addBuffs")]
    public bool? AddBuffs { get; set; }

    [JsonPropertyName("buffs")]
    public Dictionary<string, Buff[]>? Buffs { get; set; }

    [JsonPropertyName("addCrafts")]
    public bool? AddCrafts { get; set; }

    [JsonPropertyName("crafts")]
    public HideoutProduction[]? Crafts { get; set; }

    [JsonPropertyName("additionalAssortData")]
    public TraderAssort? AdditionalAssortData { get; set; }

    [JsonPropertyName("scriptedConflictingInfos")]
    public ConflictingInfos[]? ScriptedConflictingInfos { get; set; }

    [JsonPropertyName("addToPrimaryWeaponSlot")]
    public bool? AddToPrimaryWeaponSlot { get; set; }

    [JsonPropertyName("addToHolsterWeaponSlot")]
    public bool? AddToHolsterWeaponSlot { get; set; }

    [JsonPropertyName("addToQuestRewards")]
    public bool? AddToQuestRewards { get; set; }

    [JsonPropertyName("questRewards")]
    public List<QuestRewardConfig>? QuestRewards { get; set; }

    [JsonPropertyName("addToQuestAssorts")]
    public bool? AddToQuestAssorts { get; set; }

    [JsonPropertyName("questAssorts")]
    public List<QuestAssortConfig>? QuestAssorts { get; set; }

    [JsonPropertyName("presetTraders")]
    public Dictionary<string, Dictionary<string, PresetTraderConfig>>? PresetTraders { get; set; }
    public string? WeaponCloneChamberId { get; set; }
    public string? MagCloneCartridgeId { get; set; }

    public new void Validate(string itemId)
    {
        base.Validate(itemId);

        if (CopySlot == true && (CopySlots == null || CopySlots.Count == 0))
            throw new InvalidDataException($"[{itemId}] copySlots is required when copySlot is true");

        if (AddBuffs == true && (Buffs == null || Buffs.Count == 0))
            throw new InvalidDataException($"[{itemId}] buffs is required when addBuffs is true");

        if (AddCrafts == true && Crafts == null)
            throw new InvalidDataException($"[{itemId}] crafts is required when addCrafts is true");

        if (AddToQuestRewards == true && (QuestRewards == null || QuestRewards.Count == 0))
            throw new InvalidDataException($"[{itemId}] questRewards is required when addToQuestRewards is true");

        if (AddToQuestAssorts == true && (QuestAssorts == null || QuestAssorts.Count == 0))
            throw new InvalidDataException($"[{itemId}] questAssorts is required when addToQuestAssorts is true");

        if (AddToTraders == true && PresetTraders != null && PresetTraders.Count == 0)
            throw new InvalidDataException($"[{itemId}] presetTraders was provided but is empty");
    }

    public class CopySlotConfig
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

    public class ConflictingInfos
    {
        [JsonPropertyName("id")]
        public MongoId Id { get; set; }
        [JsonPropertyName("tgtSlotName")]
        public required string TgtSlotName { get; set; }
        [JsonPropertyName("itemsAddtoSlot")]
        public string[]? ItemsAddToSlot { get; set; }
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
        public string? Status { get; internal set; }
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

    public class PresetTraderConfig
    {
        [JsonPropertyName("traderId")]
        public string? TraderId { get; set; }
        [JsonPropertyName("presetId")]
        public string? PresetId { get; set; }
        [JsonPropertyName("loyal_level_items")]
        public required ConfigBarterSettings ConfigBarterSettings { get; set; }
        [JsonPropertyName("barter_scheme")]
        public required List<ConfigBarterScheme> Barters { get; set; } = new();
    }
}