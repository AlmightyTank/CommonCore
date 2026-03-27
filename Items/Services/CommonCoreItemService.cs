using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using System.Reflection;
using System.Text.Json;

namespace CommonCore.Items.Services;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public sealed class CommonCoreItemService(
    ItemFeaturePipeline itemFeaturePipeline,
    CommonCoreSettings settings,
    CoreDebugLogHelper debugLogHelper)
{
    private const string FileName = nameof(CommonCoreItemService);

    private readonly ItemFeaturePipeline _itemFeaturePipeline = itemFeaturePipeline;
    private readonly CommonCoreSettings _settings = settings;
    private readonly CoreDebugLogHelper _debugLogHelper = debugLogHelper;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public async Task<int> LoadAndProcessFolder(Assembly modAssembly, string relativeFolderPath)
    {
        const string functionName = nameof(LoadAndProcessFolder);

        var modPath = Path.GetDirectoryName(modAssembly.Location);
        if (string.IsNullOrWhiteSpace(modPath))
        {
            _debugLogHelper.LogWarning(
                FileName,
                $"Could not resolve mod path for assembly {modAssembly.FullName}",
                functionName);

            return 0;
        }

        var folderPath = Path.Combine(modPath, relativeFolderPath);

        _debugLogHelper.LogService(
            FileName,
            $"Resolved relative folder '{relativeFolderPath}' to '{folderPath}'",
            functionName);

        return await LoadAndProcessFolder(folderPath);
    }

    public async Task<int> LoadAndProcessFolders(Assembly modAssembly, params string[] relativeFolderPaths)
    {
        const string functionName = nameof(LoadAndProcessFolders);

        if (!_settings.Items.Enabled)
        {
            _debugLogHelper.LogService(
                FileName,
                "Item processing is disabled by config.",
                functionName);

            return 0;
        }

        if (relativeFolderPaths == null || relativeFolderPaths.Length == 0)
        {
            _debugLogHelper.LogWarning(
                FileName,
                "LoadAndProcessFolders called with no folder paths.",
                functionName);

            return 0;
        }

        var totalProcessed = 0;

        _debugLogHelper.LogService(
            FileName,
            $"Processing {relativeFolderPaths.Length} folder(s) for assembly '{modAssembly.GetName().Name}'",
            functionName);

        foreach (var relativeFolderPath in relativeFolderPaths)
        {
            totalProcessed += await LoadAndProcessFolder(modAssembly, relativeFolderPath);
        }

        _debugLogHelper.LogService(
            FileName,
            $"Finished processing folders. Total processed: {totalProcessed}",
            functionName);

        return totalProcessed;
    }

    public async Task<int> LoadAndProcessFolder(string folderPath)
    {
        const string functionName = nameof(LoadAndProcessFolder);

        if (!_settings.Items.Enabled)
        {
            _debugLogHelper.LogService(
                FileName,
                "Item processing is disabled by config.",
                functionName);

            return 0;
        }

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            _debugLogHelper.LogWarning(
                FileName,
                "LoadAndProcessFolder called with an empty folder path.",
                functionName);

            return 0;
        }

        if (!Directory.Exists(folderPath))
        {
            if (_settings.Items.LogMissingFolders)
            {
                _debugLogHelper.Log(
                    FileName,
                    $"Folder does not exist, skipping: {folderPath}",
                    functionName);
            }

            return 0;
        }

        var processedCount = 0;
        var files = Directory.GetFiles(folderPath, "*.json", SearchOption.AllDirectories);

        _debugLogHelper.LogService(
            FileName,
            $"Found {files.Length} json file(s) in '{folderPath}'",
            functionName);

        foreach (var filePath in files)
        {
            try
            {
                var request = await LoadRequest(filePath);
                if (request == null)
                {
                    _debugLogHelper.LogWarning(
                        FileName,
                        $"Failed to deserialize item request: {filePath}",
                        functionName);

                    if (!_settings.Items.ContinueOnFileError)
                    {
                        _debugLogHelper.LogWarning(
                            FileName,
                            "Stopping item processing because ContinueOnFileError is false.",
                            functionName);

                        break;
                    }

                    continue;
                }

                await Process(request);
                processedCount++;

                if (_settings.Items.LogProcessedFiles)
                {
                    _debugLogHelper.LogService(
                        FileName,
                        $"Processed item request from '{filePath}'",
                        functionName);
                }
            }
            catch (Exception ex)
            {
                _debugLogHelper.LogError(
                    FileName,
                    $"Failed processing item file '{filePath}': {ex}",
                    functionName);

                if (!_settings.Items.ContinueOnFileError)
                {
                    _debugLogHelper.LogWarning(
                        FileName,
                        "Stopping item processing because ContinueOnFileError is false.",
                        functionName);

                    break;
                }
            }
        }

        _debugLogHelper.LogService(
            FileName,
            $"Processed {processedCount} item file(s) from '{folderPath}'",
            functionName);

        return processedCount;
    }

    public Task Process(CommonCoreItemRequest request)
    {
        const string functionName = nameof(Process);

        ArgumentNullException.ThrowIfNull(request);

        _debugLogHelper.Log(
            FileName,
            $"Processing request for item '{TryGetRequestId(request)}'",
            functionName);

        return _itemFeaturePipeline.ProcessItemFeatures(request);
    }

    private async Task<CommonCoreItemRequest?> LoadRequest(string filePath)
    {
        const string functionName = nameof(LoadRequest);

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var request = JsonSerializer.Deserialize<CommonCoreItemRequest>(json, JsonOptions);

            if (request != null)
            {
                _debugLogHelper.Log(
                    FileName,
                    $"Successfully deserialized request from '{filePath}'",
                    functionName);
            }

            return request;
        }
        catch (Exception ex)
        {
            _debugLogHelper.LogError(
                FileName,
                $"Failed to deserialize request from '{filePath}': {ex}",
                functionName);

            return null;
        }
    }

    private static string TryGetRequestId(CommonCoreItemRequest request)
    {
        var type = request.GetType();

        var newIdProperty = type.GetProperty("NewId");
        if (newIdProperty?.GetValue(request) is string newId && !string.IsNullOrWhiteSpace(newId))
        {
            return newId;
        }

        var idProperty = type.GetProperty("Id");
        if (idProperty?.GetValue(request) is string id && !string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        return "UnknownItem";
    }
}