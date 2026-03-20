using SPTarkov.Server.Core.Models.Common;
using System.Text.Json.Serialization;

namespace CommonCore.Items.Models
{
    public class CopySlotInfo
    {
        [JsonPropertyName("id")]
        public virtual MongoId Id { get; set; }
        [JsonPropertyName("newSlotName")]
        public required virtual string NewSlotName { get; set; }
        [JsonPropertyName("tgtSlotName")]
        public virtual string? TgtSlotName { get; set; }
        [JsonPropertyName("itemsAddtoSlot")]
        public virtual string[]? ItemsAddToSlot { get; set; }
        [JsonPropertyName("required")]
        public virtual bool? Required { get; set; }
    }
}