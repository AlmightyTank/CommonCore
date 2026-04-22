using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace CommonLibExtended.Traders.Models;

public sealed class AssortBarterOverride
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("barterSettings")]
    public List<BarterSettingEntry> BarterSettings { get; set; } = [];

    public void Validate(string traderId, string assortId)
    {
        if (!Enabled)
        {
            return;
        }

        BarterSettings = BarterSettings?
            .Where(x =>
                x != null &&
                !string.IsNullOrWhiteSpace(x.Template) &&
                x.Count > 0)
            .ToList() ?? [];

        if (BarterSettings.Count == 0)
        {
            throw new InvalidOperationException(
                $"Trader '{traderId}' assort override '{assortId}' has no valid barterSettings.");
        }
    }

    public List<BarterScheme> ToBarterSchemeList()
    {
        return BarterSettings
            .Where(x =>
                x != null &&
                !string.IsNullOrWhiteSpace(x.Template) &&
                x.Count > 0)
            .Select(x => new BarterScheme
            {
                Template = x.Template,
                Count = x.Count
            })
            .ToList();
    }
}

public sealed class BarterSettingEntry
{
    [JsonPropertyName("_tpl")]
    public string Template { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public double Count { get; set; }
}