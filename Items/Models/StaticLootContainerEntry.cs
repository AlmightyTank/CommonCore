namespace CommonCore.Items.Models;

public class StaticLootContainerEntry
{
    public string ContainerName { get; set; } = string.Empty;
    public int Probability { get; set; }
    public bool ReplaceProbabilityIfExists { get; set; } = true;
}