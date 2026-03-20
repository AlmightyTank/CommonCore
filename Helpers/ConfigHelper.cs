using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace CommonCore.Helpers;

[Injectable]
public class ConfigHelper(JsonUtil jsonUtil, CoreDebugLogHelper debugLogHelper)
{
    public T? TryDeserialize<T>(string jsonContent) where T : class
    {
        try
        {
            return jsonUtil.Deserialize<T>(jsonContent);
        }
        catch
        {
            return null;
        }
    }

    public async Task<T?> LoadJsonFile<T>(string filePath) where T : class
    {
        if (!File.Exists(filePath))
        {
            debugLogHelper.LogError("ConfigHelper", $"File not found: {filePath}");
            return null;
        }

        try
        {
            var data = await jsonUtil.DeserializeFromFileAsync<T>(filePath);

            if (data != null)
                debugLogHelper.LogService("ConfigHelper",$"Loaded file: {filePath}");
            else
                debugLogHelper.LogError("ConfigHelper", $"Failed to deserialize {filePath}");

            return data;
        }
        catch (Exception ex)
        {
            debugLogHelper.LogError("ConfigHelper", $"Error loading file {filePath}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<T>> LoadAllJsonFiles<T>(string directoryPath)
    {
        var result = new List<T>();

        if (!Directory.Exists(directoryPath)) return result;

        var jsonFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".json") || f.EndsWith(".jsonc"))
            .ToArray();

        foreach (var filePath in jsonFiles)
            try
            {
                var jsonData = await jsonUtil.DeserializeFromFileAsync<T>(filePath);
                if (jsonData != null)
                {
                    result.Add(jsonData);
                    debugLogHelper.LogService("ConfigHelper",$"Loaded file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                debugLogHelper.LogError("ConfigHelper", $"Error loading file {filePath}: {ex.Message}");
            }

        return result;
    }

    private static bool ShouldSkip(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        if (fileName.Equals("assort.json", StringComparison.OrdinalIgnoreCase))
            return true;

        if (fileName.Equals("base.json", StringComparison.OrdinalIgnoreCase))
            return true;

        if (filePath.Contains($"{Path.DirectorySeparatorChar}CustomQuests{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase))
            return true;

        if (filePath.Contains($"{Path.DirectorySeparatorChar}CustomQuestZones{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public async Task<List<T>> LoadAllItemsJsonFiles<T>(string directoryPath)
    {
        var result = new List<T>();

        if (!Directory.Exists(directoryPath)) return result;

        var jsonFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".json") || f.EndsWith(".jsonc"))
            .ToArray();

        foreach (var filePath in jsonFiles)
            try
            {
                if (ShouldSkip(filePath))
                    continue;

                var jsonData = await jsonUtil.DeserializeFromFileAsync<T>(filePath);
                if (jsonData != null)
                {
                    result.Add(jsonData);
                    debugLogHelper.LogService("ConfigHelper",$"Loaded file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                debugLogHelper.LogError("ConfigHelper", $"Error loading file {filePath}: {ex.Message}");
            }

        return result;
    }


    public async Task<Dictionary<string, Dictionary<string, string>>> LoadLocalesFromDirectory(string directoryPath)
    {
        var locales = new Dictionary<string, Dictionary<string, string>>();

        var jsonFiles = Directory.GetFiles(directoryPath, "*.json", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(directoryPath, "*.jsonc", SearchOption.AllDirectories))
            .ToArray();

        foreach (var filePath in jsonFiles)
        {
            var localeCode = Path.GetFileNameWithoutExtension(filePath);

            try
            {
                var data = await jsonUtil.DeserializeFromFileAsync<Dictionary<string, string>>(filePath);

                if (data != null)
                {
                    locales[localeCode] = data;
                    debugLogHelper.LogService("ConfigHelper",$"Loaded locale file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                debugLogHelper.LogError("ConfigHelper", $"Failed to parse {filePath}: {ex.Message}");
            }
        }

        return locales;
    }

    public void SaveJsonFileSync<T>(string filePath, T data) where T : class
    {
        try
        {
            // Use JsonUtil's synchronous serialize
            var json = jsonUtil.Serialize(data, true);
            File.WriteAllText(filePath, json);
            debugLogHelper.LogService("ConfigHelper",$"Saved file: {filePath}");
        }
        catch (Exception ex)
        {
            debugLogHelper.LogError("ConfigHelper", $"Error saving file {filePath}: {ex.Message}");
            throw;
        }
    }
}