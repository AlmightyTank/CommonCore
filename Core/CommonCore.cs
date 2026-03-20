using CommonCore.Helpers;
using CommonCore.Items.Models;
using CommonCore.Items.Services;
using CommonCore.Items.Services.ItemServiceHelpers;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using System.Reflection;
using Path = System.IO.Path;

namespace CommonCore.Core;

[Injectable(InjectionType.Singleton)]
public sealed class CommonCore
{
    private readonly CommonCoreDb _db;
    private readonly CommonCoreSettings _settings;
    private readonly CompatibilityService _compatibilityService;
    private readonly CommonCoreItemService _commonCoreItemService;
    private readonly CoreQuestService _coreQuestService;
    private readonly CoreQuestZoneService _coreQuestZoneService;
    private readonly ImageRouter _imageRouter;

    public CommonCore(
        CommonCoreDb db,
        CommonCoreSettings settings,
        CompatibilityService compatibilityService,
        CommonCoreItemService commonCoreItemService,
        CoreQuestService coreQuestService,
        CoreQuestZoneService coreQuestZoneService,
        ImageRouter imageRouter)
    {
        _db = db;
        _settings = settings;
        _compatibilityService = compatibilityService;
        _commonCoreItemService = commonCoreItemService;
        _coreQuestService = coreQuestService;
        _coreQuestZoneService = coreQuestZoneService;
        _imageRouter = imageRouter;
    }

    public void OnLoad()
    {
        _db.Load();
        _compatibilityService.Initialize();
    }

    public void PostLoad()
    {
        _compatibilityService.ProcessCompatibilityInfo();
        _commonCoreItemService.ProcessDeferredModSlots();
        _commonCoreItemService.ProcessDeferredCalibers();
        _commonCoreItemService.ProcessDeferredSecureFilters();
    }

    public void SetDefaultTrader(MongoId traderId)
    {
        _settings.SetDefaultTrader(traderId);
    }

    public void CreateItem(ItemCreationRequest request)
    {
        _commonCoreItemService.Create(request);
    }

    public Task CreateCustomItems(Assembly assembly, string? relativePath = null)
    {
        return _commonCoreItemService.CreateCustomItems(assembly, relativePath);
    }

    public Task CreateCustomItemsFromDirectory(string directoryPath)
    {
        return _commonCoreItemService.CreateCustomItemsFromDirectory(directoryPath);
    }

    public Task CreateCustomQuests(Assembly assembly, string? relativePath = null)
    {
        return _coreQuestService.CreateCustomQuests(assembly, relativePath);
    }

    public Task CreateCustomQuestZones(Assembly assembly, string? relativePath = null)
    {
        return _coreQuestZoneService.CreateCustomQuestZones(assembly, relativePath);
    }

    public void RegisterQuestZone(CustomQuestZone zone)
    {
        _coreQuestZoneService.RegisterZone(zone);
    }

    public IReadOnlyList<CustomQuestZone> GetQuestZones()
    {
        return _coreQuestZoneService.GetZones();
    }
}