using System.Text.Json.Serialization;

namespace CommonCore.Items.Models
{
    public class EmptyPropSlotConfig
    {
        [JsonPropertyName("itemToAddTo")]
        public string ItemToAddTo { get; set; } = string.Empty;

        [JsonPropertyName("modSlot")]
        public string ModSlot { get; set; } = string.Empty;
    }
}