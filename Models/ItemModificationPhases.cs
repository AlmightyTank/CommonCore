namespace CommonLibExtended.Models;

[Flags]
public enum ItemModificationPhases
{
    None = 0,

    CloneCompatibilities = 1 << 0,
    SlotCopies = 1 << 1,
    PresetTraders = 1 << 2,

    EquipmentSlots = 1 << 3,
    QuestAssorts = 1 << 4,
    QuestRewards = 1 << 5,

    SpawnClones = 1 << 6,

    All = CloneCompatibilities
        | SlotCopies
        | PresetTraders
        | EquipmentSlots
        | QuestAssorts
        | QuestRewards
        | SpawnClones
}