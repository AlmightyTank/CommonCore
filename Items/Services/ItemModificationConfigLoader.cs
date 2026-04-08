using System.Reflection;
using System.Text.Json;
using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;

namespace CommonLibExtended.Items.Services;

[Injectable]
public sealed class ItemModificationConfigLoader(
    ModPathHelper modPathHelper,
    DebugLogHelper debugLogHelper,
    JsonUtil jsonUtil)
{
    private readonly ModPathHelper _modPathHelper = modPathHelper;
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly JsonUtil _jsonUtil = jsonUtil;

    public List<ItemModificationRequest> LoadFromRelativePaths(Assembly modAssembly, params string[] relativePaths)
    {
        var results = new List<ItemModificationRequest>();

        foreach (var relativePath in relativePaths)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                continue;
            }

            var fullPath = _modPathHelper.GetFullPath(modAssembly, relativePath);
            results.AddRange(LoadFromFullPath(fullPath));
        }

        return results;
    }

    public List<ItemModificationRequest> LoadFromRelativePath(Assembly modAssembly, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return [];
        }

        var fullPath = _modPathHelper.GetFullPath(modAssembly, relativePath);
        return LoadFromFullPath(fullPath);
    }

    public List<ItemModificationRequest> LoadFromFullPath(string fullPath)
    {
        var results = new List<ItemModificationRequest>();

        if (File.Exists(fullPath))
        {
            _debugLogHelper.Log(nameof(ItemModificationConfigLoader), $"Loading single file: {fullPath}");
            LoadFile(fullPath, results);
            _debugLogHelper.Log(nameof(ItemModificationConfigLoader), $"Loaded {results.Count} item request(s) from file {fullPath}");
            return results;
        }

        if (!Directory.Exists(fullPath))
        {
            _debugLogHelper.LogError(nameof(ItemModificationConfigLoader), $"Path not found: {fullPath}");
            return results;
        }

        var jsonFiles = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".jsonc", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        _debugLogHelper.Log(nameof(ItemModificationConfigLoader), $"Found {jsonFiles.Length} json/jsonc file(s) in {fullPath}");

        foreach (var filePath in jsonFiles)
        {
            var before = results.Count;
            LoadFile(filePath, results);
            var added = results.Count - before;

            _debugLogHelper.Log(nameof(ItemModificationConfigLoader), $"Loaded {added} item request(s) from {filePath}");
        }

        _debugLogHelper.Log(nameof(ItemModificationConfigLoader), $"Total loaded item request(s): {results.Count}");
        return results;
    }

    private void LoadFile(string filePath, List<ItemModificationRequest> results)
    {
        if (!File.Exists(filePath))
        {
            _debugLogHelper.LogError(nameof(ItemModificationConfigLoader), $"File not found: {filePath}");
            return;
        }

        try
        {
            var jsonContent = File.ReadAllText(filePath);

            var root = TryDeserialize<Dictionary<string, JsonElement>>(jsonContent);
            if (root == null)
            {
                _debugLogHelper.LogError(nameof(ItemModificationConfigLoader), $"Failed to deserialize root object in {filePath}");
                return;
            }

            foreach (var (itemId, element) in root)
            {
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    _debugLogHelper.LogError(nameof(ItemModificationConfigLoader), $"Blank item id in {filePath}");
                    continue;
                }

                if (element.ValueKind != JsonValueKind.Object)
                {
                    _debugLogHelper.LogError(nameof(ItemModificationConfigLoader), $"Item '{itemId}' in {filePath} is not a JSON object");
                    continue;
                }

                var request = BuildRequest(itemId, element, filePath);
                if (request == null)
                {
                    continue;
                }

                results.Add(request);
            }
        }
        catch (Exception ex)
        {
            _debugLogHelper.LogError(nameof(ItemModificationConfigLoader), $"Error loading file {filePath}: {ex}");
        }
    }

    private ItemModificationRequest? BuildRequest(string itemId, JsonElement element, string filePath)
    {
        var raw = element.GetRawText();

        var config = TryDeserialize<ItemModificationConfig>(raw);
        if (config == null)
        {
            _debugLogHelper.LogError(
                nameof(ItemModificationConfigLoader),
                $"Failed to deserialize ItemModificationConfig for '{itemId}' in {filePath}");
            return null;
        }

        var extras = TryDeserialize<ItemModificationExtras>(raw) ?? new ItemModificationExtras();

        try
        {
            config.Validate(itemId);
            extras.Validate(itemId);
        }
        catch (Exception ex)
        {
            _debugLogHelper.LogError(
                nameof(ItemModificationConfigLoader),
                $"Validation failed for '{itemId}' in {filePath}: {ex.Message}");
            return null;
        }

        return new ItemModificationRequest
        {
            ItemId = itemId,
            Config = config,
            Extras = extras,
            FilePath = filePath
        };
    }

    private T? TryDeserialize<T>(string jsonContent) where T : class
    {
        try
        {
            return _jsonUtil.Deserialize<T>(jsonContent);
        }
        catch
        {
            return null;
        }
    }
}