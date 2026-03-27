using CommonCore.Core;

namespace CommonCore.Helpers;

public class CoreDebugLogHelper(CommonCoreSettings settings)
{
    private readonly CommonCoreSettings _settings = settings;

    public bool ShouldLog(string fileName, string? functionName = null)
    {
        var debug = _settings.Debug;

        if (!debug.Enabled)
        {
            return false;
        }

        if (debug.ForceAll)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(functionName))
        {
            var functionKey = $"{fileName}.{functionName}";
            if (debug.Functions.TryGetValue(functionKey, out var functionEnabled))
            {
                return functionEnabled;
            }
        }

        if (debug.Files.TryGetValue(fileName, out var fileEnabled))
        {
            return fileEnabled;
        }

        return false;
    }

    public void Log(string fileName, string message, string? functionName = null)
    {
        if (!ShouldLog(fileName, functionName))
        {
            return;
        }

        Console.WriteLine(BuildPrefix(fileName, functionName) + message);
    }

    public void LogWarning(string fileName, string message, string? functionName = null)
    {
        if (!ShouldLog(fileName, functionName))
        {
            return;
        }

        Console.WriteLine(BuildPrefix(fileName, functionName, "WARN") + message);
    }

    public void LogError(string fileName, string message, string? functionName = null)
    {
        if (!ShouldLog(fileName, functionName))
        {
            return;
        }

        Console.WriteLine(BuildPrefix(fileName, functionName, "ERROR") + message);
    }

    public void LogService(string fileName, string message, string? functionName = null)
    {
        Log(fileName, message, functionName);
    }

    private static string BuildPrefix(string fileName, string? functionName = null, string level = "DEBUG")
    {
        var scope = string.IsNullOrWhiteSpace(functionName)
            ? fileName
            : $"{fileName}.{functionName}";

        return $"[CommonCore] [{level}] [{scope}] ";
    }
}