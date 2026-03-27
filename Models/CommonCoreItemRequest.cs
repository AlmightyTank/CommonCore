namespace CommonCore.Models;

public class CommonCoreItemRequest
{
    public required string ItemId { get; init; }
    public required CommonCoreItemConfig Config { get; init; }
}