using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Hideout;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Spt.Templates;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using System.Reflection;
using Hideout = SPTarkov.Server.Core.Models.Spt.Hideout.Hideout;

namespace CommonCore.Core;

[Injectable(InjectionType.Singleton)]
public sealed class CommonCoreDb
{
    private readonly DatabaseService _databaseService;
    private readonly ModHelper _modHelper;

    public Dictionary<MongoId, TemplateItem> Items { get; private set; } = null!;
    public Templates Templates { get; private set; } = null!;
    public Dictionary<MongoId, Trader> Traders { get; private set; } = null!;
    public Dictionary<MongoId, Quest> Quests { get; private set; } = null!;
    public Hideout Hideout { get; private set; } = null!;
    public Dictionary<string, Location> Locations { get; private set; } = null!;
    public Bots Bots { get; private set; } = null!;
    public Globals Globals { get; private set; } = null!;
    public Dictionary<MongoId, Preset> Presets { get; private set; } = null!;
    public List<HandbookItem> Handbook { get; private set; } = null!;
    public Dictionary<string, IEnumerable<Buff>> Buffs { get; private set; } = null!;
    public List<HideoutProduction> Crafts { get; private set; } = null!;
    public LocaleBase Locales { get; private set; } = null!;
    public string ModPath { get; private set; } = string.Empty;

    public bool IsLoaded { get; private set; }

    public CommonCoreDb(DatabaseService databaseService, ModHelper modHelper)
    {
        _databaseService = databaseService;
        _modHelper = modHelper;
    }

    public void Load(Assembly? assembly = null)
    {
        Items = _databaseService.GetItems();
        Traders = _databaseService.GetTraders();
        Globals = _databaseService.GetGlobals();
        Presets = Globals.ItemPresets;
        Handbook = _databaseService.GetHandbook().Items;
        Buffs = Globals.Configuration.Health.Effects.Stimulator.Buffs;
        Hideout = _databaseService.GetHideout();
        Crafts = Hideout.Production.Recipes;
        Locales = _databaseService.GetLocales();
        Quests = _databaseService.GetTables().Templates.Quests;
        Locations = _databaseService.GetTables().Locations.GetDictionary();
        Bots = _databaseService.GetBots();
        Templates = _databaseService.GetTemplates();

        ModPath = _modHelper.GetAbsolutePathToModFolder(
            assembly ?? Assembly.GetExecutingAssembly());

        IsLoaded = true;
    }

    public void EnsureLoaded(Assembly? assembly = null)
    {
        if (!IsLoaded)
        {
            Load(assembly);
        }
    }
}