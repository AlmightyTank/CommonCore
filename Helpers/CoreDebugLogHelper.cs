using CommonCore.Traders.Models;
using SPTarkov.DI.Annotations;

namespace CommonCore.Helpers;

[Injectable]
public sealed class CoreDebugLogHelper
{
    public void Initialize(string modPath, bool debugEnabled)
    {
        LogHelper.Init(modPath);
        LogHelper.IsDebugEnabled = debugEnabled;

        LogHelper.Log($"[CommonCore] Logger initialized. DebugEnabled={debugEnabled}");
    }

    public void Log(string message)
    {
        LogHelper.Log($"[CommonCore] {message}");
    }

    public void LogService(string service, string message)
    {
        LogHelper.LogDebug($"[{service}] {message}");
    }

    public void LogError(string service, string message)
    {
        LogHelper.Log($"[ERROR] [{service}] {message}");
    }

    public void LogConfig(TraderRuntimeSettings settings)
    {
        LogHelper.LogDebug("==== Trader Config ====");
        LogHelper.LogDebug($"MinLevel: {settings.MinLevel}");
        LogHelper.LogDebug($"UnlockedByDefault: {settings.UnlockedByDefault}");
        LogHelper.LogDebug($"UnlimitedStock: {settings.UnlimitedStock}");
        LogHelper.LogDebug($"RandomizeStock: {settings.RandomizeStockAvailable} ({settings.OutOfStockChance}%)");
        LogHelper.LogDebug($"PriceMultiplier: {settings.PriceMultiplier}");
        LogHelper.LogDebug($"FleaEnabled: {settings.AddTraderToFleaMarket}");
        LogHelper.LogDebug($"InsuranceCoef: {settings.InsurancePriceCoef}");
        LogHelper.LogDebug($"RepairQuality: {settings.RepairQuality}");
        LogHelper.LogDebug($"RefreshMin: {settings.TraderRefreshMin}");
        LogHelper.LogDebug($"RefreshMax: {settings.TraderRefreshMax}");
        LogHelper.LogDebug("=======================");
    }
}