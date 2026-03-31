using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Text.Json.Serialization;
using WTTServerCommonLib.Models;

namespace CommonLibExtended.Models;

public sealed class PresetDto
{
    [JsonPropertyName("_changeWeaponName")]
    public bool ChangeWeaponName { get; set; }

    [JsonPropertyName("_encyclopedia")]
    public string Encyclopedia { get; set; } = string.Empty;

    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("_name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("_parent")]
    public string Parent { get; set; } = string.Empty;

    [JsonPropertyName("_type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("_items")]
    public List<PresetItemDto> Items { get; set; } = [];

    public Preset ToPreset()
    {
        return new Preset
        {
            Id = Id,
            Name = Name,
            Parent = Parent,
            Type = Type,
            ChangeWeaponName = ChangeWeaponName,
            Encyclopedia = Encyclopedia,
            Items = Items.Select(x => x.ToPresetItem()).ToList()
        };
    }
}

public sealed class PresetItemDto
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("_tpl")]
    public string Tpl { get; set; } = string.Empty;

    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }

    [JsonPropertyName("slotId")]
    public string? SlotId { get; set; }

    [JsonPropertyName("upd")]
    public Upd? Upd { get; set; }

    public Item ToPresetItem()
    {
        return new Item
        {
            Id = Id,
            Template = Tpl,
            ParentId = ParentId,
            SlotId = SlotId,
            Upd = Upd
        };
    }
}