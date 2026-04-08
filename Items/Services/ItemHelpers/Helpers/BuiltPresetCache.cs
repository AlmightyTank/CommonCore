using CommonLibExtended.Items.Models;
using SPTarkov.DI.Annotations;

namespace CommonLibExtended.Items.Services.ItemHelpers.Helpers;

[Injectable]
public sealed class BuiltPresetCache
{
    private readonly Dictionary<string, BuiltPresetResult> _byPresetId =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, BuiltPresetResult> _byAssortId =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, BuiltPresetResult> _byRootBuiltItemId =
        new(StringComparer.OrdinalIgnoreCase);

    public void Store(string presetId, string assortId, BuiltPresetResult builtPreset)
    {
        if (builtPreset == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(presetId))
        {
            _byPresetId[presetId] = builtPreset;
        }

        if (!string.IsNullOrWhiteSpace(assortId))
        {
            _byAssortId[assortId] = builtPreset;
        }

        var rootBuiltItemId = builtPreset.RootBuiltItemId?.ToString();
        if (!string.IsNullOrWhiteSpace(rootBuiltItemId))
        {
            _byRootBuiltItemId[rootBuiltItemId] = builtPreset;
        }
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

    public BuiltPresetResult? GetByRootBuiltItemId(string rootBuiltItemId)
    {
        if (string.IsNullOrWhiteSpace(rootBuiltItemId))
        {
            return null;
        }

        _byRootBuiltItemId.TryGetValue(rootBuiltItemId, out var builtPreset);
        return builtPreset;
    }

    public string? ResolveFinalOfferIdFromPresetId(string presetId)
    {
        var builtPreset = GetByPresetId(presetId);
        return builtPreset?.RootBuiltItemId?.ToString();
    }

    public string? ResolveFinalOfferIdFromAssortId(string assortId)
    {
        var builtPreset = GetByAssortId(assortId);
        return builtPreset?.RootBuiltItemId?.ToString();
    }

    public string? ResolveFinalOfferId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var builtPreset = GetByAssortId(id)
            ?? GetByPresetId(id)
            ?? GetByRootBuiltItemId(id);

        return builtPreset?.RootBuiltItemId?.ToString() ?? id;
    }

    public bool ContainsPresetId(string presetId)
    {
        return !string.IsNullOrWhiteSpace(presetId) && _byPresetId.ContainsKey(presetId);
    }

    public bool ContainsAssortId(string assortId)
    {
        return !string.IsNullOrWhiteSpace(assortId) && _byAssortId.ContainsKey(assortId);
    }

    public void Clear()
    {
        _byPresetId.Clear();
        _byAssortId.Clear();
        _byRootBuiltItemId.Clear();
    }
}