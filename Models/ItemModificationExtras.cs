using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using System.Text.Json.Serialization;
using WTTServerCommonLib.Models;

namespace CommonLibExtended.Models;

public sealed class ItemModificationExtras
{
    [JsonPropertyName("copySlot")]
    public bool? CopySlot { get; set; }

    [JsonPropertyName("copySlotsInfo")]
    public List<CopySlotConfig>? CopySlots { get; set; }

    [JsonPropertyName("ammoCloneCompatibility")]
    public bool? AmmoCloneCompatibility { get; set; }

    [JsonPropertyName("weaponCloneChamberCompatibility")]
    public bool? WeaponCloneChamberCompatibility { get; set; }

    [JsonPropertyName("magCloneCartridgeCompatibility")]
    public bool? MagCloneCartridgeCompatibility { get; set; }

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
    public bool AddToQuestAssorts { get; set; }

    [JsonPropertyName("questAssorts")]
    public QuestAssortConfig[] QuestAssorts { get; set; } = [];

    [JsonPropertyName("presetTraders")]
    public Dictionary<string, Dictionary<string, PresetTraderConfig>>? PresetTraders { get; set; }

    [JsonPropertyName("weaponCloneChamberId")]
    public string? WeaponCloneChamberId { get; set; }

    [JsonPropertyName("magCloneCartridgeId")]
    public string? MagCloneCartridgeId { get; set; }

    [JsonPropertyName("addSpawnsInSamePlacesAsOrigin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AddSpawnsInSamePlacesAsOrigin { get; set; }

    [JsonPropertyName("spawnWeightComparedToOrigin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? SpawnWeightComparedToOrigin { get; set; }

    public void Validate(string itemId)
    {
        if (CopySlot == true && (CopySlots == null || CopySlots.Count == 0))
            throw new InvalidDataException($"[{itemId}] copySlots is required when copySlot is true");

        if (AddToQuestRewards == true && (QuestRewards == null || QuestRewards.Count == 0))
            throw new InvalidDataException($"[{itemId}] questRewards is required when addToQuestRewards is true");

        if (AddToQuestAssorts == true && QuestAssorts == null)
            throw new InvalidDataException($"[{itemId}] questAssorts is required when addToQuestAssorts is true");

        if (PresetTraders != null && PresetTraders.Count == 0)
            throw new InvalidDataException($"[{itemId}] presetTraders was provided but is empty");
    }
}

public class CopySlotConfig
{
    [JsonPropertyName("id")]
    public required virtual string Id { get; set; }

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
    public required string Id { get; set; }

    [JsonPropertyName("tgtSlotName")]
    public required string TgtSlotName { get; set; }

    [JsonPropertyName("itemsAddtoSlot")]
    public string[]? ItemsAddToSlot { get; set; }
}

public class QuestRewardConfig
{
    [JsonPropertyName("questId")]
    public string? QuestId { get; set; }

    [JsonPropertyName("rewardType")]
    public string? RewardType { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("findInRaid")]
    public bool FindInRaid { get; set; }

    [JsonPropertyName("isHidden")]
    public bool IsHidden { get; set; }

    [JsonPropertyName("currencyTpl")]
    public string? CurrencyTpl { get; set; }

    [JsonPropertyName("presetId")]
    public string? PresetId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public sealed class RewardDisplayItem
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("_tpl")]
    public string Template { get; set; } = string.Empty;
}

public sealed class QuestAssortConfig
{
    [JsonPropertyName("questId")]
    public string QuestId { get; set; } = string.Empty;

    [JsonPropertyName("traderId")]
    public string? TraderId { get; set; }

    [JsonPropertyName("assortId")]
    public string? AssortId { get; set; }

    [JsonPropertyName("presetId")]
    public string? PresetId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public sealed class PresetTraderConfig
{
    [JsonPropertyName("presetId")]
    public string PresetId { get; set; } = string.Empty;

    [JsonPropertyName("barter_scheme")]
    public List<ConfigBarterScheme> Barters { get; set; } = [];

    [JsonPropertyName("loyal_level_items")]
    public TraderOfferSettings LoyalLevelItems { get; set; } = new();
}

public sealed class ConfigBarterScheme
{
    [JsonPropertyName("template")]
    public string Template { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public sealed class TraderOfferSettings
{
    [JsonPropertyName("loyalLevel")]
    public int LoyalLevel { get; set; } = 1;

    [JsonPropertyName("unlimitedCount")]
    public bool UnlimitedCount { get; set; } = true;

    [JsonPropertyName("stackObjectsCount")]
    public int StackObjectsCount { get; set; } = 999999;

    [JsonPropertyName("buyRestrictionMax")]
    public int BuyRestrictionMax { get; set; } = 0;
}