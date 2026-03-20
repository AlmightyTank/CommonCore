using System.Text.Json.Serialization;

namespace CommonCore.Items.Models
{
    public class ConfigTraderScheme
    {
        [JsonPropertyName("loyal_level_items")]
        public required ConfigBarterSettings ConfigBarterSettings { get; set; }

        [JsonPropertyName("barter_scheme")]
        public required List<ConfigBarterScheme> Barters { get; set; } = new();
    }
}