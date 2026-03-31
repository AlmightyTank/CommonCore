using CommonLibExtended.Models;
using System.Reflection;

namespace CommonLibExtended.Services;

internal sealed class ItemProcessingSession
{
    public required Assembly Assembly { get; init; }
    public required string CacheKey { get; init; }
    public required List<ItemModificationRequest> Requests { get; init; }

    public bool CompatibilityInitialized { get; set; }
    public bool CompatibilityFinalized { get; set; }
    public ItemModificationPhases ProcessedPhases { get; set; } = ItemModificationPhases.None;
}