using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
using System.Reflection;

namespace CommonCore.Items.Services;

[Injectable(InjectionType.Singleton)]
public sealed class CoreQuestZoneService(
    ModHelper modHelper,
    ISptLogger<CoreQuestZoneService> logger,
    ConfigHelper configHelper)
{
    private readonly object _sync = new();
    private readonly List<CustomQuestZone> _zones = [];

    private readonly ModHelper _modHelper = modHelper;
    private readonly ISptLogger<CoreQuestZoneService> _logger = logger;
    private readonly ConfigHelper _configHelper = configHelper;

    public async Task CreateCustomQuestZones(Assembly assembly, string? relativePath = null)
    {
        string modPath = _modHelper.GetAbsolutePathToModFolder(assembly);
        string finalPath = Path.Combine(modPath, relativePath ?? Path.Combine("db", "CustomQuestZones"));

        if (!Directory.Exists(finalPath))
        {
            _logger.Info($"No CustomQuestZones directory found at {finalPath}");
            return;
        }

        List<CustomQuestZone> zones = await LoadZoneFiles(finalPath);
        RegisterZones(zones);
    }

    public void RegisterZone(CustomQuestZone zone)
    {
        if (zone == null)
        {
            _logger.Warning("Attempted to register a null quest zone");
            return;
        }

        if (string.IsNullOrWhiteSpace(zone.ZoneName))
        {
            _logger.Warning("Attempted to register a quest zone with no ZoneName");
            return;
        }

        lock (_sync)
        {
            if (_zones.Any(x => string.Equals(x.ZoneName, zone.ZoneName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.Warning($"Quest zone '{zone.ZoneName}' is already registered, skipping");
                return;
            }

            _zones.Add(zone);
            _logger.Info($"Registered quest zone '{zone.ZoneName}'. Total zones: {_zones.Count}");
        }
    }

    public IReadOnlyList<CustomQuestZone> GetZones()
    {
        lock (_sync)
        {
            return _zones.ToList();
        }
    }

    private void RegisterZones(IEnumerable<CustomQuestZone> zones)
    {
        var added = 0;

        lock (_sync)
        {
            foreach (var zone in zones)
            {
                if (zone == null || string.IsNullOrWhiteSpace(zone.ZoneName))
                {
                    continue;
                }

                if (_zones.Any(x => string.Equals(x.ZoneName, zone.ZoneName, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.Warning($"Quest zone '{zone.ZoneName}' is already registered, skipping");
                    continue;
                }

                _zones.Add(zone);
                added++;
            }

            _logger.Info($"Registered {added} quest zones. Total zones: {_zones.Count}");
        }
    }

    private async Task<List<CustomQuestZone>> LoadZoneFiles(string directory)
    {
        var loadedZones = new List<CustomQuestZone>();

        try
        {
            var zoneLists = await _configHelper.LoadAllJsonFiles<List<CustomQuestZone>>(directory);

            foreach (var fileZones in zoneLists)
            {
                if (fileZones.Count == 0)
                {
                    continue;
                }

                loadedZones.AddRange(fileZones);
                _logger.Info($"Loaded {fileZones.Count} quest zones from one file");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error loading custom quest zones from {directory}: {ex.Message}");
        }

        return loadedZones;
    }
}