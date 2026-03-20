using CommonCore.Core;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Utils;

namespace CommonCore.Items.Services;

[Injectable(InjectionType.Singleton)]
public sealed class ContentService
{
    private readonly ISptLogger<ContentService> _logger;
    private readonly CommonCoreDb _db;

    public ContentService(
        ISptLogger<ContentService> logger,
        CommonCoreDb db)
    {
        _logger = logger;
        _db = db;
    }

    public void AddPreset(Preset preset)
    {
        foreach (var item in preset.Items)
        {
            item.ParentId = item.ParentId?.ToLower();
        }

        if (!_db.Presets.TryAdd(preset.Id, preset))
        {
            _logger.Error($"Preset {preset.Id} already exists");
        }
    }

    public void AddBuffs(Dictionary<string, Buff[]> buffs)
    {
        foreach (var (key, value) in buffs)
        {
            if (value == null || value.Length == 0)
            {
                continue;
            }

            if (_db.Buffs.TryGetValue(key, out var existing))
            {
                _db.Buffs[key] = existing
                    .Concat(value)
                    .Where(x => x != null)
                    .GroupBy(GetBuffMergeKey)
                    .Select(g => g.First())
                    .ToArray();
            }
            else
            {
                _db.Buffs[key] = value;
            }
        }
    }

    public void AddCrafts(HideoutProduction[] crafts)
    {
        foreach (var craft in crafts)
        {
            if (craft == null)
            {
                continue;
            }

            if (_db.Crafts.Any(x => string.Equals(x.Id, craft.Id, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.Warning($"Craft {craft.Id} already exists, skipping.");
                continue;
            }

            _db.Crafts.Add(craft);
        }
    }

    public void AddLocales(Dictionary<string, Dictionary<string, string>> locales)
    {
        foreach (var lang in locales)
        {
            if (!_db.Locales.Global.TryGetValue(lang.Key, out var lazyLocale))
            {
                _logger.Warning($"Locale '{lang.Key}' not found, skipping.");
                continue;
            }

            lazyLocale.AddTransformer(localeData =>
            {
                foreach (var entry in lang.Value)
                {
                    if (localeData!.ContainsKey(entry.Key))
                    {
                        localeData[entry.Key] = entry.Value;
                    }
                    else
                    {
                        localeData.Add(entry.Key, entry.Value);
                    }
                }

                return localeData;
            });
        }
    }

    private static string GetBuffMergeKey(Buff buff)
    {
        var appliesTo = buff.AppliesTo == null
            ? string.Empty
            : string.Join(",", buff.AppliesTo);

        return string.Join("|",
            buff.BuffType ?? string.Empty,
            buff.Chance.ToString(System.Globalization.CultureInfo.InvariantCulture),
            buff.Delay.ToString(System.Globalization.CultureInfo.InvariantCulture),
            buff.Duration.ToString(System.Globalization.CultureInfo.InvariantCulture),
            buff.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            buff.AbsoluteValue.ToString(),
            buff.SkillName ?? string.Empty,
            appliesTo);
    }
}