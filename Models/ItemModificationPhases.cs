namespace CommonLibExtended.Models;

[Flags]
public enum ItemModificationPhases
{
    None = 0,

    CloneCompatibilities = 1 << 0,
    SlotCopies = 1 << 1,
    PresetTraders = 1 << 2,

    EquipmentSlots = 1 << 5,
    QuestAssorts = 1 << 6,
    QuestRewards = 1 << 7,
    SpawnClones = 1 << 8,
    QuestConditionClones = 1 << 9,
    QuestConditions = 1 << 10,

    All = CloneCompatibilities
        | SlotCopies
        | PresetTraders
        | EquipmentSlots
        | QuestAssorts
        | QuestRewards
        | SpawnClones
        | QuestConditionClones
        | QuestConditions
}