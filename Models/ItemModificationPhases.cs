namespace CommonLibExtended.Models;

[Flags]
public enum ItemModificationPhases
{
    None = 0,
    CloneCompatibilities = 1 << 0,
    SlotCopies = 1 << 1,
    PresetTraders = 1 << 2,
    Buffs = 1 << 3,
    Crafts = 1 << 4,
    EquipmentSlots = 1 << 5,
    QuestAssorts = 1 << 6,
    QuestRewards = 1 << 7,

    All = CloneCompatibilities
        | SlotCopies
        | PresetTraders
        | Buffs
        | Crafts
        | EquipmentSlots
        | QuestAssorts
        | QuestRewards
}