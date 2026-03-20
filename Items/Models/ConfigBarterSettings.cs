using System.Text.Json.Serialization;

namespace CommonCore.Items.Models
{
    public class ConfigBarterSettings
    {
        [JsonPropertyName("loyalLevel")]
        public required int LoyalLevel { get; set; }

        [JsonPropertyName("unlimitedCount")]
        public required bool UnlimitedCount { get; set; }

        [JsonPropertyName("stackObjectsCount")]
        public required int StackObjectsCount { get; set; }

        [JsonPropertyName("buyRestrictionMax")]
        public int? BuyRestrictionMax { get; set; }
    }
}