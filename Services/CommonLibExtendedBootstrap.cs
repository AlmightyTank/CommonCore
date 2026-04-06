using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Logging;
using SPTarkov.Server.Core.Models.Utils;
using System.Reflection;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;

namespace CommonLibExtended.Services;

[Injectable(InjectionType.Singleton)]
public sealed class CommonLibExtendedBootstrap(
    ISptLogger<CommonLibExtendedBootstrap> logger,
    ItemModificationConfigLoader itemModificationConfigLoader,
    CompatibilityService compatibilityService,
    ItemModificationService itemModificationService,
    ModPathHelper modPathHelper,
    PresetRegistryLoader presetRegistryLoader)
{
    private readonly ISptLogger<CommonLibExtendedBootstrap> _logger = logger;
    private readonly ItemModificationConfigLoader _itemModificationConfigLoader = itemModificationConfigLoader;
    private readonly CompatibilityService _compatibilityService = compatibilityService;
    private readonly ItemModificationService _itemModificationService = itemModificationService;
    private readonly ModPathHelper _modPathHelper = modPathHelper;
    private readonly PresetRegistryLoader _presetRegistryLoader = presetRegistryLoader;

    private readonly Dictionary<string, ItemProcessingSession> _sessions =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task RegisterWeaponPresets(Assembly assembly, string relativePath)
    {
        try
        {
            _presetRegistryLoader.LoadFromPath(assembly, relativePath);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error registering weapon presets from {relativePath}: {ex}");
        }

        await Task.CompletedTask;
    }

    public async Task ProcessCloneCompatibilities(Assembly assembly, params string[] relativePaths)
    {
        var session = GetOrCreateSession(assembly, relativePaths);
        EnsureCompatibilityInitialized(session);

        if (session.Requests.Count == 0)
        {
            await Task.CompletedTask;
            return;
        }

        ProcessPhase(session, ItemModificationPhases.CloneCompatibilities);
        await Task.CompletedTask;
    }

    public async Task ProcessSlotCopies(Assembly assembly, params string[] relativePaths)
    {
        var session = GetOrCreateSession(assembly, relativePaths);
        EnsureCompatibilityInitialized(session);

        if (session.Requests.Count == 0)
        {
            await Task.CompletedTask;
            return;
        }

        ProcessPhase(session, ItemModificationPhases.SlotCopies);
        await Task.CompletedTask;
    }

    public async Task ProcessPresetTraders(Assembly assembly, params string[] relativePaths)
    {
        var session = GetOrCreateSession(assembly, relativePaths);
        EnsureCompatibilityInitialized(session);

        if (session.Requests.Count == 0)
        {
            await Task.CompletedTask;
            return;
        }

        ProcessPhase(session, ItemModificationPhases.PresetTraders);
        await Task.CompletedTask;
    }

    public async Task ProcessEquipmentSlots(Assembly assembly, params string[] relativePaths)
    {
        var session = GetOrCreateSession(assembly, relativePaths);
        EnsureCompatibilityInitialized(session);

        if (session.Requests.Count == 0)
        {
            await Task.CompletedTask;
            return;
        }

        ProcessPhase(session, ItemModificationPhases.EquipmentSlots);
        await Task.CompletedTask;
    }

    public async Task ProcessQuestAssorts(Assembly assembly, params string[] relativePaths)
    {
        var session = GetOrCreateSession(assembly, relativePaths);
        EnsureCompatibilityInitialized(session);

        if (session.Requests.Count == 0)
        {
            await Task.CompletedTask;
            return;
        }

        ProcessPhase(session, ItemModificationPhases.QuestAssorts);
        await Task.CompletedTask;
    }

    public async Task ProcessQuestRewards(Assembly assembly, params string[] relativePaths)
    {
        var session = GetOrCreateSession(assembly, relativePaths);
        EnsureCompatibilityInitialized(session);

        if (session.Requests.Count == 0)
        {
            await Task.CompletedTask;
            return;
        }

        ProcessPhase(session, ItemModificationPhases.QuestRewards);
        await Task.CompletedTask;
    }

    public async Task ProcessSpawnClones(Assembly assembly, params string[] relativePaths)
    {
        var session = GetOrCreateSession(assembly, relativePaths);
        EnsureCompatibilityInitialized(session);

        if (session.Requests.Count == 0)
        {
            await Task.CompletedTask;
            return;
        }

        ProcessPhase(session, ItemModificationPhases.SpawnClones);
        await Task.CompletedTask;
    }

    public async Task ProcessQuestConditionClones(Assembly assembly, params string[] relativePaths)
    {
        var session = GetOrCreateSession(assembly, relativePaths);
        EnsureCompatibilityInitialized(session);

        if (session.Requests.Count == 0)
        {
            await Task.CompletedTask;
            return;
        }

        ProcessPhase(session, ItemModificationPhases.QuestConditionClones);
        await Task.CompletedTask;
    }

    public async Task ProcessQuestConditions(Assembly assembly, params string[] relativePaths)
    {
        var session = GetOrCreateSession(assembly, relativePaths);
        EnsureCompatibilityInitialized(session);

        if (session.Requests.Count == 0)
        {
            await Task.CompletedTask;
            return;
        }

        ProcessPhase(session, ItemModificationPhases.QuestConditions);
        await Task.CompletedTask;
    }

    public async Task ProcessTheRest(Assembly assembly, params string[] relativePaths)
    {
        var session = GetOrCreateSession(assembly, relativePaths);
        EnsureCompatibilityInitialized(session);

        if (session.Requests.Count == 0)
        {
            FinalizeSession(session);
            await Task.CompletedTask;
            return;
        }

        var remainingPhases = ItemModificationPhases.All & ~session.ProcessedPhases;

        if (remainingPhases == ItemModificationPhases.None)
        {
            if (_logger.IsLogEnabled(LogLevel.Debug))
            {
                _logger.Debug($"No remaining phases for session {session.CacheKey}");
            }

            FinalizeSession(session);
            await Task.CompletedTask;
            return;
        }

        ProcessPhases(session, remainingPhases);
        FinalizeSession(session);

        await Task.CompletedTask;
    }

    public async Task CreateCustomItems(Assembly assembly, params string[] relativePaths)
    {
        var session = GetOrCreateSession(assembly, relativePaths);
        EnsureCompatibilityInitialized(session);

        if (session.Requests.Count == 0)
        {
            FinalizeSession(session);
            await Task.CompletedTask;
            return;
        }

        ProcessPhases(session, ItemModificationPhases.All);
        FinalizeSession(session);

        await Task.CompletedTask;
    }

    private ItemProcessingSession GetOrCreateSession(Assembly assembly, params string[] relativePaths)
    {
        var normalizedPaths = NormalizePaths(relativePaths);
        var cacheKey = BuildCacheKey(assembly, normalizedPaths);

        if (_sessions.TryGetValue(cacheKey, out var existingSession))
        {
            return existingSession;
        }

        var requests = LoadRequestsInternal(assembly, normalizedPaths);

        var session = new ItemProcessingSession
        {
            Assembly = assembly,
            CacheKey = cacheKey,
            Requests = requests
        };

        _sessions[cacheKey] = session;
        return session;
    }

    private List<ItemModificationRequest> LoadRequestsInternal(Assembly assembly, string[] relativePaths)
    {
        if (relativePaths.Length == 0)
        {
            _logger.Warning("No relative paths supplied to CommonLibExtendedBootstrap.");
            return [];
        }

        var validRelativePaths = new List<string>();

        foreach (var relativePath in relativePaths)
        {
            var fullPath = _modPathHelper.GetFullPath(assembly, relativePath);

            if (!Directory.Exists(fullPath) && !File.Exists(fullPath))
            {
                _logger.Warning($"Skipping missing path: {fullPath}");
                continue;
            }

            validRelativePaths.Add(relativePath);
        }

        if (validRelativePaths.Count == 0)
        {
            _logger.Warning("No valid relative paths were found.");
            return [];
        }

        var requests = _itemModificationConfigLoader.LoadFromRelativePaths(assembly, validRelativePaths.ToArray());

        if (requests.Count == 0)
        {
            var modRoot = _modPathHelper.GetModRoot(assembly);
            _logger.Warning($"No valid item configs found for mod root {modRoot}");
        }
        else if (_logger.IsLogEnabled(LogLevel.Debug))
        {
            _logger.Debug($"Loaded {requests.Count} request(s) from {validRelativePaths.Count} path(s)");
        }

        return requests;
    }

    private void EnsureCompatibilityInitialized(ItemProcessingSession session)
    {
        if (session.CompatibilityInitialized)
        {
            return;
        }

        _compatibilityService.Initialize();
        session.CompatibilityInitialized = true;

        if (_logger.IsLogEnabled(LogLevel.Debug))
        {
            _logger.Debug($"Initialized compatibility for session {session.CacheKey}");
        }
    }

    private void FinalizeSession(ItemProcessingSession session)
    {
        if (session.CompatibilityInitialized && !session.CompatibilityFinalized)
        {
            _compatibilityService.ProcessCompatibilityInfo();
            session.CompatibilityFinalized = true;

            if (_logger.IsLogEnabled(LogLevel.Debug))
            {
                _logger.Debug($"Finalized compatibility for session {session.CacheKey}");
            }
        }

        _sessions.Remove(session.CacheKey);
    }

    private static readonly ItemModificationPhases[] OrderedPhases =
    [
    ItemModificationPhases.CloneCompatibilities,
    ItemModificationPhases.SlotCopies,
    ItemModificationPhases.PresetTraders,
    ItemModificationPhases.EquipmentSlots,
    ItemModificationPhases.QuestAssorts,
    ItemModificationPhases.QuestRewards,
    ItemModificationPhases.SpawnClones,
    ItemModificationPhases.QuestConditionClones,
    ItemModificationPhases.QuestConditions
    ];

    private void ProcessPhases(ItemProcessingSession session, ItemModificationPhases phases)
    {
        foreach (var phase in OrderedPhases)
        {
            if (phases.HasFlag(phase))
            {
                ProcessPhase(session, phase);
            }
        }
    }

    private void ProcessPhase(ItemProcessingSession session, ItemModificationPhases phase)
    {
        if (session.ProcessedPhases.HasFlag(phase))
        {
            if (_logger.IsLogEnabled(LogLevel.Debug))
            {
                _logger.Debug($"Skipping already processed phase {phase} for session {session.CacheKey}");
            }

            return;
        }

        switch (phase)
        {
            case ItemModificationPhases.CloneCompatibilities:
                _itemModificationService.ProcessCloneCompatibilities(session.Requests);
                break;

            case ItemModificationPhases.SlotCopies:
                _itemModificationService.ProcessSlotCopies(session.Requests);
                break;

            case ItemModificationPhases.PresetTraders:
                _itemModificationService.ProcessPresetTraders(session.Requests);
                break;

            case ItemModificationPhases.EquipmentSlots:
                _itemModificationService.ProcessEquipmentSlots(session.Requests);
                break;

            case ItemModificationPhases.QuestAssorts:
                _itemModificationService.ProcessQuestAssorts(session.Requests);
                break;

            case ItemModificationPhases.QuestRewards:
                _itemModificationService.ProcessQuestRewards(session.Requests);
                break;

            case ItemModificationPhases.SpawnClones:
                _itemModificationService.ProcessSpawnClones(session.Requests);
                break;

            case ItemModificationPhases.QuestConditionClones:
                _itemModificationService.ProcessQuestConditionClones(session.Requests);
                break;

            case ItemModificationPhases.QuestConditions:
                _itemModificationService.ProcessQuestConditions(session.Requests);
                break;

            default:
                _logger.Warning($"Unknown phase requested: {phase}");
                return;
        }

        session.ProcessedPhases |= phase;

        if (_logger.IsLogEnabled(LogLevel.Debug))
        {
            _logger.Debug($"Processed phase {phase} for session {session.CacheKey}");
        }
    }

    private static string[] NormalizePaths(IEnumerable<string> relativePaths)
    {
        return relativePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Replace('\\', '/').Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string BuildCacheKey(Assembly assembly, IEnumerable<string> normalizedPaths)
    {
        return $"{assembly.FullName}::{string.Join("|", normalizedPaths)}";
    }
}