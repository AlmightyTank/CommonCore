using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using WTTServerCommonLib.Models;

namespace CommonLibExtended.Services;

[Injectable]
public sealed class PresetRegistryService
{
    private readonly Dictionary<string, Preset> _presetsById =
        new(StringComparer.OrdinalIgnoreCase);

    public void Store(Preset preset)
    {
        if (preset == null || string.IsNullOrWhiteSpace(preset.Id))
        {
            return;
        }

        _presetsById[preset.Id] = preset;
    }

    public void StoreMany(IEnumerable<Preset>? presets)
    {
        if (presets == null)
        {
            return;
        }

        foreach (var preset in presets)
        {
            Store(preset);
        }
    }

    public Preset? GetById(string? presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
        {
            return null;
        }

        _presetsById.TryGetValue(presetId, out var preset);
        return preset;
    }

    public bool Contains(string? presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
        {
            return false;
        }

        return _presetsById.ContainsKey(presetId);
    }

    public int Count => _presetsById.Count;

    public void Clear()
    {
        _presetsById.Clear();
    }
}