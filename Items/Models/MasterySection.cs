using System.Text.Json.Serialization;

namespace CommonCore.Items.Models
{
    public class MasterySection
    {
        [JsonPropertyName("Name")]
        public required virtual string Name { get; set; }

        [JsonPropertyName("Templates")]
        public virtual string[] Templates { get; set; } = [];

        [JsonPropertyName("Level2")]
        public virtual int Level2 { get; set; }

        [JsonPropertyName("Level3")]
        public virtual int Level3 { get; set; }
    }
}