using SPTarkov.Server.Core.Models.Common;
using System.Text.Json.Serialization;

namespace CommonCore.Items.Models
{
    public class ConflictingInfos
    {

        [JsonPropertyName("id")]
        public virtual MongoId Id { get; set; }
        [JsonPropertyName("tgtSlotName")]
        public virtual required string TgtSlotName { get; set; }
        [JsonPropertyName("itemsAddtoSlot")]
        public virtual string[]? ItemsAddToSlot { get; set; }
    }
}