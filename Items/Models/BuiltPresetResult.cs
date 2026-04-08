using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace CommonLibExtended.Items.Models;

public sealed class BuiltPresetResult
{
    public required string SourcePresetId { get; init; }
    public required string RootSourceItemId { get; init; }
    public required string RootBuiltItemId { get; init; }
    public required List<Item> Items { get; init; }
    public required Dictionary<string, string> IdMap { get; init; }
}