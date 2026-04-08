using System.Text.Json.Serialization;

namespace CommonLibExtended.Traders.Models;

public sealed class CustomTraderSettings
{
    [JsonPropertyName("minLevel")]
    public int MinLevel { get; set; } = 1;

    [JsonPropertyName("unlockedByDefault")]
    public bool UnlockedByDefault { get; set; }

    [JsonPropertyName("traderRefreshMin")]
    public int TraderRefreshMin { get; set; } = 1800;

    [JsonPropertyName("traderRefreshMax")]
    public int TraderRefreshMax { get; set; } = 3600;

    [JsonPropertyName("addTraderToFleaMarket")]
    public bool AddTraderToFleaMarket { get; set; } = true;

    [JsonPropertyName("insurancePriceCoef")]
    public double InsurancePriceCoef { get; set; } = 50;

    [JsonPropertyName("repairQuality")]
    public double RepairQuality { get; set; } = 0.8;

    [JsonPropertyName("randomizeStockAvailable")]
    public bool RandomizeStockAvailable { get; set; }

    [JsonPropertyName("outOfStockChance")]
    public int OutOfStockChance { get; set; } = 15;

    [JsonPropertyName("unlimitedStock")]
    public bool UnlimitedStock { get; set; } = true;

    [JsonPropertyName("priceMultiplier")]
    public double PriceMultiplier { get; set; } = 1.0;

    [JsonPropertyName("debugLogging")]
    public bool DebugLogging { get; set; }

    public void Validate(string traderId)
    {
        if (MinLevel < 1)
        {
            throw new InvalidDataException($"[{traderId}] minLevel must be >= 1");
        }

        if (TraderRefreshMin < 1)
        {
            throw new InvalidDataException($"[{traderId}] traderRefreshMin must be >= 1");
        }

        if (TraderRefreshMax < TraderRefreshMin)
        {
            throw new InvalidDataException($"[{traderId}] traderRefreshMax must be >= traderRefreshMin");
        }

        if (InsurancePriceCoef < 0)
        {
            throw new InvalidDataException($"[{traderId}] insurancePriceCoef must be >= 0");
        }

        if (RepairQuality < 0 || RepairQuality > 1)
        {
            throw new InvalidDataException($"[{traderId}] repairQuality must be between 0 and 1");
        }

        if (OutOfStockChance < 0 || OutOfStockChance > 100)
        {
            throw new InvalidDataException($"[{traderId}] outOfStockChance must be between 0 and 100");
        }

        if (PriceMultiplier <= 0)
        {
            throw new InvalidDataException($"[{traderId}] priceMultiplier must be > 0");
        }
    }
}