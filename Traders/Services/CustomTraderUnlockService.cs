using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Servers;

namespace CommonLibExtended.Services;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public sealed class CustomTraderUnlockService(
    DebugLogHelper debugLogHelper,
    SaveServer saveServer) : IOnLoad, IDisposable
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly SaveServer _saveServer = saveServer;

    private readonly Dictionary<string, CustomTraderUnlockRegistration> _registrations =
        new(StringComparer.OrdinalIgnoreCase);

    private System.Threading.Timer? _timer;
    private bool _started;

    public void RegisterTrader(string traderId, int minLevel, bool unlockedByDefault)
    {
        if (string.IsNullOrWhiteSpace(traderId))
        {
            _debugLogHelper.LogError("CustomTraderUnlockService", "Cannot register trader unlock with empty traderId");
            return;
        }

        _registrations[traderId] = new CustomTraderUnlockRegistration
        {
            TraderId = traderId,
            MinLevel = Math.Max(minLevel, 1),
            UnlockedByDefault = unlockedByDefault
        };

        _debugLogHelper.LogService(
            "CustomTraderUnlockService",
            $"Registered unlock rule for trader {traderId} (MinLevel={minLevel}, UnlockedByDefault={unlockedByDefault})");
    }

    public Task OnLoad()
    {
        if (_started)
        {
            return Task.CompletedTask;
        }

        _started = true;

        _timer = new System.Threading.Timer(
            _ => CheckAllProfiles(),
            null,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(10));

        _debugLogHelper.LogService("CustomTraderUnlockService", "Started trader unlock timer");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public void CheckAllProfiles()
    {
        if (_registrations.Count == 0)
        {
            return;
        }

        try
        {
            var profiles = _saveServer.GetProfiles();

            foreach (var (sessionId, profile) in profiles)
            {
                CheckAndUnlockRegisteredTraders(sessionId, profile);
            }
        }
        catch (Exception ex)
        {
            _debugLogHelper.LogError("CustomTraderUnlockService", $"Error checking profiles: {ex.Message}");
        }
    }

    private void CheckAndUnlockRegisteredTraders(string sessionId, object profile)
    {
        if (profile == null)
        {
            return;
        }

        try
        {
            var characters = GetMemberValue(profile, "Characters")
                             ?? GetMemberValue(profile, "CharacterData");

            if (characters == null)
            {
                return;
            }

            var pmcProfile = GetMemberValue(characters, "Pmc")
                             ?? GetMemberValue(characters, "PmcData")
                             ?? GetMemberValue(characters, "Pmcs");

            if (pmcProfile == null)
            {
                return;
            }

            var info = GetMemberValue(pmcProfile, "Info");
            if (info == null)
            {
                return;
            }

            var levelValue = GetMemberValue(info, "Level");
            var playerLevel = levelValue != null ? Convert.ToInt32(levelValue) : 0;

            var tradersInfo = GetMemberValue(pmcProfile, "TradersInfo");
            if (tradersInfo is not System.Collections.IDictionary tradersDict)
            {
                return;
            }

            foreach (var registration in _registrations.Values)
            {
                CheckAndUnlockTrader(sessionId, tradersDict, registration, playerLevel);
            }
        }
        catch (Exception ex)
        {
            _debugLogHelper.LogError("CustomTraderUnlockService", $"Exception during unlock check: {ex.Message}");
        }
    }

    private void CheckAndUnlockTrader(
        string sessionId,
        System.Collections.IDictionary tradersDict,
        CustomTraderUnlockRegistration registration,
        int playerLevel)
    {
        object? targetKey = null;

        foreach (var key in tradersDict.Keys)
        {
            if (string.Equals(key?.ToString(), registration.TraderId, StringComparison.OrdinalIgnoreCase))
            {
                targetKey = key;
                break;
            }
        }

        if (targetKey == null)
        {
            return;
        }

        var traderInfo = tradersDict[targetKey];
        if (traderInfo == null)
        {
            return;
        }

        var unlockedValue = GetMemberValue(traderInfo, "Unlocked");
        var isUnlocked = unlockedValue != null && (bool)unlockedValue;

        if (registration.UnlockedByDefault)
        {
            if (!isUnlocked)
            {
                SetUnlocked(traderInfo, true);
                _debugLogHelper.LogService(
                    "CustomTraderUnlockService",
                    $"Force-unlocked trader {registration.TraderId} for session {sessionId}");
            }

            return;
        }

        if (playerLevel >= registration.MinLevel && !isUnlocked)
        {
            SetUnlocked(traderInfo, true);

            _debugLogHelper.LogService(
                "CustomTraderUnlockService",
                $"Unlocked trader {registration.TraderId} for session {sessionId} at level {playerLevel}");
        }
    }

    private static object? GetMemberValue(object target, string name)
    {
        if (target == null)
        {
            return null;
        }

        var type = target.GetType();
        var flags = System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.IgnoreCase;

        var prop = type.GetProperty(name, flags);
        if (prop != null)
        {
            return prop.GetValue(target);
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
        var flags = System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.IgnoreCase;

        var prop = type.GetProperty("Unlocked", flags);
        if (prop != null)
        {
            prop.SetValue(target, value);
            return;
        }

        var field = type.GetField("Unlocked", flags);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}