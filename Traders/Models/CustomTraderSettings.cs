using System.Text.Json.Serialization;

namespace CommonLibExtended.Traders.Models;

public sealed class CustomTraderSettings
{
    [JsonPropertyName("debugLogging")]
    public bool DebugLogging { get; set; } = false;

    [JsonPropertyName("minLevel")]
    public int MinLevel { get; set; } = 1;

    [JsonPropertyName("unlockedByDefault")]
    public bool UnlockedByDefault { get; set; } = false;

    [JsonPropertyName("traderRefreshMin")]
    public int TraderRefreshMin { get; set; } = 1800;

    [JsonPropertyName("traderRefreshMax")]
    public int TraderRefreshMax { get; set; } = 3600;

    [JsonPropertyName("addTraderToFleaMarket")]
    public bool AddTraderToFleaMarket { get; set; } = false;

    [JsonPropertyName("insurancePriceCoef")]
    public double InsurancePriceCoef { get; set; } = 1.0;

    [JsonPropertyName("repairQuality")]
    public double RepairQuality { get; set; } = 1.0;

    [JsonPropertyName("randomizeStockAvailable")]
    public bool RandomizeStockAvailable { get; set; } = false;

    [JsonPropertyName("outOfStockChance")]
    public int OutOfStockChance { get; set; } = 0;

    [JsonPropertyName("unlimitedStock")]
    public bool UnlimitedStock { get; set; } = false;

    [JsonPropertyName("priceMultiplier")]
    public double PriceMultiplier { get; set; } = 1.0;

    [JsonPropertyName("enableCurrency")]
    public bool EnableCurrency { get; set; } = false;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "RUB";

    [JsonPropertyName("assortBarterOverrides")]
    public AssortBarterOverrideContainer AssortBarterOverrides { get; set; } = new();

    public void Validate(string traderId)
    {
        if (string.IsNullOrWhiteSpace(traderId))
        {
            throw new InvalidOperationException("Trader settings validation failed because traderId was null or empty.");
        }

        if (MinLevel < 1)
        {
            MinLevel = 1;
        }

        if (TraderRefreshMin < 1)
        {
            TraderRefreshMin = 1;
        }

        if (TraderRefreshMax < TraderRefreshMin)
        {
            TraderRefreshMax = TraderRefreshMin;
        }

        if (InsurancePriceCoef < 0)
        {
            InsurancePriceCoef = 0;
        }

        if (RepairQuality < 0)
        {
            RepairQuality = 0;
        }

        if (OutOfStockChance < 0)
        {
            OutOfStockChance = 0;
        }

        if (OutOfStockChance > 100)
        {
            OutOfStockChance = 100;
        }

        if (PriceMultiplier <= 0)
        {
            PriceMultiplier = 1.0;
        }

        Currency = (Currency ?? "RUB").Trim().ToUpperInvariant();
        if (Currency is not ("RUB" or "USD" or "EUR"))
        {
            Currency = "RUB";
        }

        AssortBarterOverrides ??= new AssortBarterOverrideContainer();
        AssortBarterOverrides.Build(traderId);
    }
}