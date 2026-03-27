using CommonCore.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace CommonCore.Helpers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public sealed class BuiltPresetCache
{
    private readonly Dictionary<string, BuiltPresetResult> _byPresetId =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, BuiltPresetResult> _byAssortId =
        new(StringComparer.OrdinalIgnoreCase);

    public void Store(string presetId, string assortId, BuiltPresetResult builtPreset)
    {
        _byPresetId[presetId] = builtPreset;
        _byAssortId[assortId] = builtPreset;
    }

    public BuiltPresetResult? GetByPresetId(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
        {
            return null;
        }

        _byPresetId.TryGetValue(presetId, out var builtPreset);
        return builtPreset;
    }

    public BuiltPresetResult? GetByAssortId(string assortId)
    {
        if (string.IsNullOrWhiteSpace(assortId))
        {
            return null;
        }

        _byAssortId.TryGetValue(assortId, out var builtPreset);
        return builtPreset;
    }
}