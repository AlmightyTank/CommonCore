using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommonLibExtended.Traders.Models;

public sealed class AssortBarterOverrideContainer
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonExtensionData]
    public Dictionary<string, JsonElement> RawOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    public Dictionary<string, AssortBarterOverride> ParsedOverrides { get; private set; }
        = new(StringComparer.OrdinalIgnoreCase);

    public void Build(string traderId)
    {
        ParsedOverrides.Clear();

        if (!Enabled || RawOverrides == null || RawOverrides.Count == 0)
        {
            return;
        }

        foreach (var (key, value) in RawOverrides)
        {
            if (string.Equals(key, "enabled", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var parsed = value.Deserialize<AssortBarterOverride>();

                if (parsed == null)
                {
                    continue;
                }

                parsed.Validate(traderId, key);
                ParsedOverrides[key] = parsed;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Trader '{traderId}' assort override '{key}' failed to deserialize: {ex.Message}",
                    ex);
            }
        }
    }
}