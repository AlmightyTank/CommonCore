using WTTServerCommonLib.Models;

namespace CommonLibExtended.Models;

public sealed class ItemModificationRequest
{
    public required string ItemId { get; init; }
    public required CustomItemConfig Config { get; init; }
    public required ItemModificationExtras Extras { get; init; }
    public required string FilePath { get; init; }
}