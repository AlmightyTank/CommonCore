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

    // NEW
    [JsonPropertyName("useBasePriceGeneration")]
    public bool UseBasePriceGeneration { get; set; } = false;

    [JsonPropertyName("basePriceSource")]
    public string BasePriceSource { get; set; } = "Handbook";

    [JsonPropertyName("basePriceMultiplier")]
    public double BasePriceMultiplier { get; set; } = 1.0;

    [JsonPropertyName("basePriceFloor")]
    public double BasePriceFloor { get; set; } = 1.0;

    [JsonPropertyName("repriceCashOffersOnly")]
    public bool RepriceCashOffersOnly { get; set; } = true;

    [JsonPropertyName("useFleaPricing")]
    public bool UseFleaPricing { get; set; } = false;

    [JsonPropertyName("fleaWeight")]
    public double FleaWeight { get; set; } = 0.3;

    [JsonPropertyName("handbookWeight")]
    public double HandbookWeight { get; set; } = 0.7;

    [JsonPropertyName("useAttachmentPricing")]
    public bool UseAttachmentPricing { get; set; } = true;

    [JsonPropertyName("useAttachmentWeighting")]
    public bool UseAttachmentWeighting { get; set; } = false;

    [JsonPropertyName("minAttachmentPrice")]
    public double MinAttachmentPrice { get; set; } = 1000;

    [JsonPropertyName("attachmentCategoryMultipliers")]
    public Dictionary<string, double> AttachmentCategoryMultipliers { get; set; } = [];

    [JsonPropertyName("rebuildItemBarters")]
    public bool RebuildItemBarters { get; set; } = false;

    [JsonPropertyName("barterValueTolerance")]
    public double BarterValueTolerance { get; set; } = 0.15;

    [JsonPropertyName("maxBarterComponents")]
    public int MaxBarterComponents { get; set; } = 3;

    [JsonPropertyName("preferredBarterTpls")]
    public List<string> PreferredBarterTpls { get; set; } = [];

    [JsonPropertyName("categoryBasePrices")]
    public Dictionary<string, double> CategoryBasePrices { get; set; } = [];

    [JsonPropertyName("rarityMultipliers")]
    public Dictionary<string, double>? RarityMultipliers { get; set; } = [];

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

        if (BasePriceMultiplier <= 0)
        {
            throw new InvalidDataException($"[{traderId}] basePriceMultiplier must be > 0");
        }

        if (BasePriceFloor < 0)
        {
            throw new InvalidDataException($"[{traderId}] basePriceFloor must be >= 0");
        }

        if (BarterValueTolerance < 0 || BarterValueTolerance > 1)
        {
            throw new InvalidDataException($"[{traderId}] barterValueTolerance must be between 0 and 1");
        }

        if (MaxBarterComponents < 1)
        {
            throw new InvalidDataException($"[{traderId}] maxBarterComponents must be >= 1");
        }

        if (CategoryBasePrices.Values.Any(x => x < 0))
        {
            throw new InvalidDataException($"[{traderId}] categoryBasePrices cannot contain negative values");
        }

        if (RarityMultipliers.Values.Any(x => x <= 0))
        {
            throw new InvalidDataException($"[{traderId}] rarityMultipliers must be > 0");
        }
    }
}