using CommonCore.Constants;
using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using System.Text.Json;
using Path = System.IO.Path;

namespace CommonCore.Items.Services;

[Injectable(InjectionType.Singleton)]
public sealed class CoreQuestService(
    ISptLogger<CoreQuestService> logger,
    CommonCoreDb db,
    ConfigServer configServer,
    ImageRouter imageRouter,
    ModHelper modHelper,
    ConfigHelper configHelper,
    JsonUtil jsonUtil)
{
    private static readonly string[] ValidImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];

    private readonly ISptLogger<CoreQuestService> _logger = logger;
    private readonly CommonCoreDb _db = db;
    private readonly ConfigServer _configServer = configServer;
    private readonly ImageRouter _imageRouter = imageRouter;
    private readonly ModHelper _modHelper = modHelper;
    private readonly ConfigHelper _configHelper = configHelper;
    private readonly JsonUtil _jsonUtil = jsonUtil;

    private Dictionary<string, CustomQuestTimeWindow> _timeWindows = [];

    public async Task CreateCustomQuests(Assembly assembly, string? relativePath = null)
    {
        string modPath = _modHelper.GetAbsolutePathToModFolder(assembly);
        string finalPath = Path.Combine(modPath, relativePath ?? Path.Combine("db", "CustomQuests"));

        await ImportQuestTimeConfig(finalPath);
        await ImportQuestSideConfig(finalPath);
        await LoadAllTraderQuests(finalPath);
    }

    private async Task LoadAllTraderQuests(string basePath)
    {
        if (!Directory.Exists(basePath))
        {
            _logger.Warning($"Quest base directory not found: {basePath}");
            return;
        }

        foreach (string traderDir in Directory.GetDirectories(basePath))
        {
            string traderKey = Path.GetFileName(traderDir);
            string? traderId = ResolveTraderId(traderKey);

            if (string.IsNullOrWhiteSpace(traderId))
            {
                _logger.Warning($"Unknown trader key '{traderKey}' and not a valid Mongo ID");
                continue;
            }

            await LoadTraderQuestDirectory(traderId, traderDir);
        }
    }

    private string? ResolveTraderId(string traderKey)
    {
        if (ItemMaps.TraderMap.TryGetValue(traderKey.ToLowerInvariant(), out MongoId mappedTraderId))
        {
            _logger.Info($"Mapped trader key '{traderKey}' to ID '{mappedTraderId}'");
            return mappedTraderId.ToString();
        }

        if (traderKey.IsValidMongoId())
        {
            return traderKey;
        }

        return null;
    }

    private async Task LoadTraderQuestDirectory(string traderId, string traderDir)
    {
        _logger.Info($"Loading quests for trader {traderId} from {traderDir}");

        List<Dictionary<MongoId, Quest>> questFiles = await LoadQuestFiles(Path.Combine(traderDir, "Quests"));
        List<Dictionary<string, Dictionary<MongoId, MongoId>>> assortFiles = await LoadAssortFiles(Path.Combine(traderDir, "QuestAssort"));
        List<string> imageFiles = LoadImageFiles(Path.Combine(traderDir, "Images"));

        ImportQuestData(questFiles, traderId);
        ImportQuestAssortData(assortFiles, traderId);
        await ImportLocaleData(traderId, traderDir);
        ImportImageData(imageFiles, traderId);
    }

    private async Task<List<Dictionary<MongoId, Quest>>> LoadQuestFiles(string questsDir)
    {
        var result = new List<Dictionary<MongoId, Quest>>();

        if (!Directory.Exists(questsDir))
        {
            return result;
        }

        try
        {
            var files = await _configHelper.LoadAllJsonFiles<Dictionary<MongoId, Quest>>(questsDir);

            foreach (var file in files)
            {
                if (file.Count > 0)
                {
                    result.Add(file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error scanning quest files in {questsDir}: {ex.Message}");
        }

        return result;
    }

    private async Task<List<Dictionary<string, Dictionary<MongoId, MongoId>>>> LoadAssortFiles(string assortDir)
    {
        var result = new List<Dictionary<string, Dictionary<MongoId, MongoId>>>();

        if (!Directory.Exists(assortDir))
        {
            return result;
        }

        try
        {
            var files = await _configHelper.LoadAllJsonFiles<Dictionary<string, Dictionary<MongoId, MongoId>>>(assortDir);

            foreach (var file in files)
            {
                if (file.Count > 0)
                {
                    result.Add(file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error scanning assort files in {assortDir}: {ex.Message}");
        }

        return result;
    }

    private List<string> LoadImageFiles(string imageDir)
    {
        if (!Directory.Exists(imageDir))
        {
            return [];
        }

        try
        {
            return Directory.GetFiles(imageDir)
                .Where(file => ValidImageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error loading images from {imageDir}: {ex.Message}");
            return [];
        }
    }

    private void ImportQuestData(List<Dictionary<MongoId, Quest>> questFiles, string traderId)
    {
        int count = 0;

        foreach (var file in questFiles)
        {
            foreach (var (questId, quest) in file)
            {
                if (!questId.IsValidMongoId())
                {
                    _logger.Warning($"{traderId}: Invalid quest ID '{questId}', skipping");
                    continue;
                }

                if (_timeWindows.TryGetValue(questId, out var timeWindow) && !IsWithin(timeWindow))
                {
                    continue;
                }

                _db.Quests[questId] = quest;
                count++;
            }
        }

        _logger.Info($"{traderId}: Loaded {count} quests");
    }

    private void ImportQuestAssortData(List<Dictionary<string, Dictionary<MongoId, MongoId>>> assortFiles, string traderId)
    {
        if (!_db.Traders.TryGetValue(new MongoId(traderId), out var trader) || trader == null)
        {
            _logger.Warning($"Trader {traderId} not found in database, cannot import quest assort");
            return;
        }

        int count = 0;

        foreach (var file in assortFiles)
        {
            foreach (var (stage, stageData) in file)
            {
                if (!trader.QuestAssort.TryGetValue(stage, out var stageAssort))
                {
                    stageAssort = [];
                    trader.QuestAssort[stage] = stageAssort;
                }

                foreach (var (questId, assortId) in stageData)
                {
                    stageAssort[questId] = assortId;
                    count++;
                }
            }
        }

        _logger.Info($"{traderId}: Loaded {count} quest assort entries");
    }

    private async Task ImportLocaleData(string traderId, string traderDir)
    {
        string localesPath = Path.Combine(traderDir, "Locales");

        try
        {
            var locales = await _configHelper.LoadLocalesFromDirectory(localesPath);
            if (locales.Count == 0)
            {
                return;
            }

            var fallback = locales.TryGetValue("en", out var english)
                ? english
                : locales.Values.FirstOrDefault();

            if (fallback == null)
            {
                return;
            }

            foreach (var (localeCode, lazyLocale) in _db.Locales.Global)
            {
                lazyLocale.AddTransformer(localeData =>
                {
                    if (localeData == null)
                    {
                        return localeData;
                    }

                    var selectedLocale = locales.GetValueOrDefault(localeCode, fallback);

                    foreach (var (key, value) in selectedLocale)
                    {
                        localeData[key] = value;
                    }

                    return localeData;
                });
            }

            _logger.Info($"{traderId}: Registered locale transformers for {locales.Count} locale sets");
        }
        catch (Exception ex)
        {
            _logger.Error($"{traderId}: Error loading quest locales: {ex.Message}");
        }
    }

    private void ImportImageData(List<string> imageFiles, string traderId)
    {
        if (imageFiles.Count == 0)
        {
            return;
        }

        foreach (var imagePath in imageFiles)
        {
            try
            {
                string imageName = Path.GetFileNameWithoutExtension(imagePath);
                _imageRouter.AddRoute($"/files/quest/icon/{imageName}", imagePath);
            }
            catch (Exception ex)
            {
                _logger.Warning($"{traderId}: Failed to register image {imagePath}: {ex.Message}");
            }
        }

        _logger.Info($"{traderId}: Registered {imageFiles.Count} quest images");
    }

    private async Task ImportQuestTimeConfig(string basePath)
    {
        string[] fileNames = ["QuestTimeData.json", "QuestTimeData.jsonc"];

        string? configPath = fileNames
            .Select(name => Path.Combine(basePath, name))
            .FirstOrDefault(File.Exists);

        if (configPath == null)
        {
            return;
        }

        try
        {
            _timeWindows = await _jsonUtil.DeserializeFromFileAsync<Dictionary<string, CustomQuestTimeWindow>>(configPath) ?? [];
            _logger.Info($"Loaded {_timeWindows.Count} quest time windows from {Path.GetFileName(configPath)}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error loading {Path.GetFileName(configPath)}: {ex.Message}");
        }
    }

    private async Task ImportQuestSideConfig(string basePath)
    {
        string[] fileNames = ["QuestSideData.json", "QuestSideData.jsonc"];

        string? configPath = fileNames
            .Select(name => Path.Combine(basePath, name))
            .FirstOrDefault(File.Exists);

        if (configPath == null)
        {
            return;
        }

        try
        {
            string content = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<CustomQuestSideConfig>(content);
            if (config == null)
            {
                return;
            }

            var questConfig = _configServer.GetConfig<QuestConfig>();

            foreach (string questId in config.UsecOnlyQuests)
            {
                if (questId.IsValidMongoId())
                {
                    questConfig.UsecOnlyQuests.Add(questId);
                }
            }

            foreach (string questId in config.BearOnlyQuests)
            {
                if (questId.IsValidMongoId())
                {
                    questConfig.BearOnlyQuests.Add(questId);
                }
            }

            _logger.Info($"Loaded quest side config from {Path.GetFileName(configPath)}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error loading {Path.GetFileName(configPath)}: {ex.Message}");
        }
    }

    private static bool IsWithin(CustomQuestTimeWindow window)
    {
        DateTime now = DateTime.Now;
        int year = now.Year;

        DateTime start = new(year, window.StartMonth, window.StartDay);
        DateTime end = new(year, window.EndMonth, window.EndDay);

        if (end < start)
        {
            if (now.Month < window.StartMonth)
            {
                start = start.AddYears(-1);
            }
            else
            {
                end = end.AddYears(1);
            }
        }

        return now >= start && now <= end;
    }
}