using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace CommonCore.Traders.Models;

public class CustomQuestSideConfig
{
    [JsonPropertyName("usecOnlyQuests")] public required HashSet<string> UsecOnlyQuests { get; set; }

    [JsonPropertyName("bearOnlyQuests")] public required HashSet<string> BearOnlyQuests { get; set; }
}