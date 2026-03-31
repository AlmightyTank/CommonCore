using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using System.Reflection;
using System.Text.Json;
using WTTServerCommonLib.Models;

namespace CommonLibExtended.Services;

[Injectable]
public sealed class PresetRegistryLoader(
    DebugLogHelper debugLogHelper,
    ModPathHelper modPathHelper,
    PresetRegistryService presetRegistryService)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly ModPathHelper _modPathHelper = modPathHelper;
    private readonly PresetRegistryService _presetRegistryService = presetRegistryService;

    public void LoadFromPath(Assembly assembly, string relativePath)
    {
        if (assembly == null)
        {
            _debugLogHelper.LogError(nameof(PresetRegistryLoader), "Assembly is null");
            return;
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            _debugLogHelper.LogError(nameof(PresetRegistryLoader), "Relative path is null or empty");
            return;
        }

        var fullPath = _modPathHelper.GetFullPath(assembly, relativePath);

        if (!Directory.Exists(fullPath))
        {
            _debugLogHelper.LogError(nameof(PresetRegistryLoader), $"Preset folder not found: {fullPath}");
            return;
        }

        var files = Directory.GetFiles(fullPath, "*.json", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            _debugLogHelper.Log(nameof(PresetRegistryLoader), $"No preset json files found in {fullPath}");
            return;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var registeredCount = 0;

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);

                if (string.IsNullOrWhiteSpace(json))
                {
                    _debugLogHelper.LogError(nameof(PresetRegistryLoader), $"Preset file is empty: {file}");
                    continue;
                }

                if (TryLoadPresetDtoDictionary(json, options, out var dictionaryCount))
                {
                    registeredCount += dictionaryCount;
                    _debugLogHelper.LogService(
                        nameof(PresetRegistryLoader),
                        $"Registered {dictionaryCount} preset(s) from {Path.GetFileName(file)}");
                    continue;
                }

                if (TryLoadPresetDtoList(json, options, out var listCount))
                {
                    registeredCount += listCount;
                    _debugLogHelper.LogService(
                        nameof(PresetRegistryLoader),
                        $"Registered {listCount} preset(s) from {Path.GetFileName(file)}");
                    continue;
                }

                if (TryLoadSinglePresetDto(json, options, out var presetId))
                {
                    registeredCount++;
                    _debugLogHelper.LogService(
                        nameof(PresetRegistryLoader),
                        $"Registered preset {presetId} from {Path.GetFileName(file)}");
                    continue;
                }

                _debugLogHelper.LogError(
                    nameof(PresetRegistryLoader),
                    $"Could not deserialize preset file {file} as Dictionary<string, PresetDto>, List<PresetDto>, or PresetDto");
            }
            catch (Exception ex)
            {
                _debugLogHelper.LogError(
                    nameof(PresetRegistryLoader),
                    $"Failed loading preset file {file}: {ex}");
            }
        }

        _debugLogHelper.Log(
            nameof(PresetRegistryLoader),
            $"Preset registry now contains {_presetRegistryService.Count} preset(s); added {registeredCount} from {relativePath}");
    }

    private bool TryLoadPresetDtoDictionary(string json, JsonSerializerOptions options, out int count)
    {
        count = 0;

        try
        {
            var presetDictionary = JsonSerializer.Deserialize<Dictionary<string, PresetDto>>(json, options);
            if (presetDictionary == null || presetDictionary.Count == 0)
            {
                return false;
            }

            foreach (var (_, presetDto) in presetDictionary)
            {
                if (presetDto == null || string.IsNullOrWhiteSpace(presetDto.Id))
                {
                    continue;
                }

                var preset = presetDto.ToPreset();
                _presetRegistryService.Store(preset);
                count++;
            }

            return count > 0;
        }
        catch
        {
            return false;
        }
    }

    private bool TryLoadPresetDtoList(string json, JsonSerializerOptions options, out int count)
    {
        count = 0;

        try
        {
            var presetDtos = JsonSerializer.Deserialize<List<PresetDto>>(json, options);
            if (presetDtos is not { Count: > 0 })
            {
                return false;
            }

            foreach (var presetDto in presetDtos)
            {
                if (presetDto == null || string.IsNullOrWhiteSpace(presetDto.Id))
                {
                    continue;
                }

                var preset = presetDto.ToPreset();
                _presetRegistryService.Store(preset);
                count++;
            }

            return count > 0;
        }
        catch
        {
            return false;
        }
    }

    private bool TryLoadSinglePresetDto(string json, JsonSerializerOptions options, out string presetId)
    {
        presetId = string.Empty;

        try
        {
            var presetDto = JsonSerializer.Deserialize<PresetDto>(json, options);
            if (presetDto == null || string.IsNullOrWhiteSpace(presetDto.Id))
            {
                return false;
            }

            var preset = presetDto.ToPreset();
            _presetRegistryService.Store(preset);
            presetId = preset.Id;
            return true;
        }
        catch
        {
            return false;
        }
    }
}