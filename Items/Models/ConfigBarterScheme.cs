using SPTarkov.Server.Core.Models.Enums;
using System.Text.Json.Serialization;

namespace CommonCore.Items.Models
{
    public class ConfigBarterScheme
    {
        [JsonPropertyName("count")]
        public virtual double? Count { get; set; }

        [JsonPropertyName("_tpl")]
        public virtual string Template { get; set; } = string.Empty;

        [JsonPropertyName("onlyFunctional")]
        public virtual bool? OnlyFunctional { get; set; }

        [JsonPropertyName("sptQuestLocked")]
        public virtual bool? SptQuestLocked { get; set; }

        [JsonPropertyName("level")]
        public virtual int? Level { get; set; }

        [JsonPropertyName("side")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public virtual DogtagExchangeSide? Side { get; set; }
    }
}