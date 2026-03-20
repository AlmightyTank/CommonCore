namespace CommonCore.Traders.Services;

using CommonCore.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Servers;
using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public sealed class TraderProfileMonitorService : IOnLoad, IDisposable
{
    private readonly SaveServer _saveServer;
    private readonly CoreDebugLogHelper _debugLogService;
    private Timer? _timer;

    // Runtime values set by TraderUnlockCoordinator
    public static string TraderId { get; set; } = string.Empty;
    public static int MinLevelRequired { get; set; } = 1;
    public static bool EnableLevelLock { get; set; } = false;
    public static bool ForceUnlock { get; set; } = false;

    public TraderProfileMonitorService(SaveServer saveServer, CoreDebugLogHelper debugLogService)
    {
        _saveServer = saveServer;
        _debugLogService = debugLogService;
    }

    public Task OnLoad()
    {
        _timer?.Dispose();
        _timer = null;

        _debugLogService.LogService("TraderProfileMonitorService",
                    $"TraderId='{TraderId}', EnableLevelLock={EnableLevelLock}, ForceUnlock={ForceUnlock}, MinLevelRequired={MinLevelRequired}");

        if (string.IsNullOrWhiteSpace(TraderId))
        {
            _debugLogService.LogService("TraderProfileMonitorService",
                    $"TraderId is empty. Monitor will not start.");
            return Task.CompletedTask;
        }

        if (EnableLevelLock)
        {
            _debugLogService.LogService("TraderProfileMonitorService",
                    $"Level monitoring active for trader '{TraderId}'. Required level: {MinLevelRequired}");

            CheckAllProfiles();

            _timer = new Timer(
                _ => CheckAllProfiles(),
                null,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(10));

            _debugLogService.LogService("TraderProfileMonitorService",
                    $"Poll timer started (10 seconds).");
        }
        else if (ForceUnlock)
        {
            _debugLogService.LogService("TraderProfileMonitorService",
                    $"Force unlocking trader '{TraderId}' for all profiles.");

            CheckAllProfiles(forceUnlock: true);
        }
        else
        {
            _debugLogService.LogService("TraderProfileMonitorService",
                    $"Monitoring skipped. No unlock mode enabled.");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;

        _debugLogService.LogService("TraderProfileMonitorService",
                    $"Timer disposed.");
    }

    public void CheckAllProfiles(bool forceUnlock = false)
    {
        if ((!EnableLevelLock && !forceUnlock && !ForceUnlock) || string.IsNullOrWhiteSpace(TraderId))
        {
            _debugLogService.LogService("TraderProfileMonitorService",
                    $"Profile check skipped due to disabled state or missing TraderId.");
            return;
        }

        try
        {
            var profiles = _saveServer.GetProfiles();
            _debugLogService.LogService("TraderProfileMonitorService",
                    $"Checking {profiles.Count} profiles. ForceUnlock={forceUnlock || ForceUnlock}");

            foreach (var (sessionId, profile) in profiles)
            {
                CheckAndUnlockTrader(sessionId, profile, forceUnlock || ForceUnlock);
            }
        }
        catch (Exception ex)
        {
            _debugLogService.LogService("TraderProfileMonitorService",
                $"Error checking profiles for trader '{TraderId}': {ex}");
        }
    }

    public void CheckAndUnlockTrader(string sessionId, object profile, bool forceUnlock = false)
    {
        if (profile == null || string.IsNullOrWhiteSpace(TraderId))
        {
            return;
        }

        try
        {
            var characters = GetMemberValue(profile, "Characters")
                             ?? GetMemberValue(profile, "CharacterData");

            if (characters == null)
            {
                _debugLogService.LogService("TraderProfileMonitorService",
                    $"Session '{sessionId}': character container missing.");
                return;
            }

            var pmcProfile = GetMemberValue(characters, "Pmc")
                             ?? GetMemberValue(characters, "PmcData")
                             ?? GetMemberValue(characters, "Pmcs");

            if (pmcProfile == null)
            {
                _debugLogService.LogService("TraderProfileMonitorService",
                    $"Session '{sessionId}': PMC profile missing.");
                return;
            }

            var info = GetMemberValue(pmcProfile, "Info");
            if (info == null)
            {
                _debugLogService.LogService("TraderProfileMonitorService",
                    $"Session '{sessionId}': Info missing.");
                return;
            }

            var levelValue = GetMemberValue(info, "Level");
            var playerLevel = levelValue != null ? Convert.ToInt32(levelValue) : 0;

            var tradersInfo = GetMemberValue(pmcProfile, "TradersInfo");
            if (tradersInfo is not IDictionary tradersDict)
            {
                _debugLogService.LogService("TraderProfileMonitorService",
                    $"Session '{sessionId}': TradersInfo missing or not a dictionary.");
                return;
            }

            object? targetKey = null;
            foreach (var key in tradersDict.Keys)
            {
                if (string.Equals(key?.ToString(), TraderId, StringComparison.OrdinalIgnoreCase))
                {
                    targetKey = key;
                    break;
                }
            }

            if (targetKey == null)
            {
                _debugLogService.LogService("TraderProfileMonitorService",
                    $"Session '{sessionId}': trader '{TraderId}' not found in TradersInfo.");
                return;
            }

            var traderInfo = tradersDict[targetKey];
            if (traderInfo == null)
            {
                _debugLogService.LogService("TraderProfileMonitorService",
                    $"Session '{sessionId}': trader info object is null.");
                return;
            }

            var unlockedValue = GetMemberValue(traderInfo, "Unlocked");
            var isUnlocked = unlockedValue is bool unlocked && unlocked;

            _debugLogService.LogService("TraderProfileMonitorService",
                    $"Session '{sessionId}': level={playerLevel}, unlocked={isUnlocked}, forceUnlock={forceUnlock}");

            if (forceUnlock)
            {
                if (!isUnlocked)
                {
                    SetUnlocked(traderInfo, true);
                    _debugLogService.LogService("TraderProfileMonitorService",
                        "Forced unlock for trader '{TraderId}' on session '{sessionId}'.");
                }

                return;
            }

            if (EnableLevelLock && playerLevel >= MinLevelRequired && !isUnlocked)
            {
                SetUnlocked(traderInfo, true);
                _debugLogService.LogService("TraderProfileMonitorService",
                    $"Session '{sessionId}' reached level {playerLevel}. Trader '{TraderId}' unlocked.");
            }
        }
        catch (Exception ex)
        {
            _debugLogService.LogService("TraderProfileMonitorService",
                $"Exception while processing trader '{TraderId}' for session '{sessionId}': {ex}");
        }
    }

    private static object? GetMemberValue(object target, string name)
    {
        var type = target.GetType();

        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.IgnoreCase;

        var property = type.GetProperty(name, flags);
        if (property != null)
        {
            return property.GetValue(target);
        }

        var field = type.GetField(name, flags);
        if (field != null)
        {
            return field.GetValue(target);
        }

        return null;
    }

    private static void SetUnlocked(object target, bool value)
    {
        var type = target.GetType();

        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.IgnoreCase;

        var property = type.GetProperty("Unlocked", flags);
        if (property != null)
        {
            property.SetValue(target, value);
            return;
        }

        var field = type.GetField("Unlocked", flags);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}