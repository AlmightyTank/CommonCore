using SPTarkov.Server.Core.Models.Common;

namespace CommonLibExtended.Constants;

public static class Maps
{
    public static readonly Dictionary<string, MongoId> TraderMap = 
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "mechanic", "5a7c2eca46aef81a7ca2145d" },
            { "skier", "58330581ace78e27b8b10cee" },
            { "peacekeeper", "5935c25fb3acc3127c3d8cd9" },
            { "therapist", "54cb57776803fa99248b456e" },
            { "prapor", "54cb50c76803fa8b248b4571" },
            { "jaeger", "5c0647fdd443bc2504c2d371" },
            { "ragman", "5ac3b934156ae10c4430e83c" },
            { "fence", "579dc571d53a0658a154fbec" },
            { "ref", "6617beeaa9cfa777ca915b7c"},
            { "tony", "698f904cd0fa772942d237c7" }
        };
}