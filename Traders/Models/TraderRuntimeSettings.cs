namespace CommonCore.Traders.Models;

public sealed class TraderRuntimeSettings
{
    public bool DebugLogging { get; set; } = true;

    public int MinLevel { get; set; } = 1;
    public bool UnlockedByDefault { get; set; } = true;
    public bool AddTraderToFleaMarket { get; set; } = true;

    public bool UnlimitedStock { get; set; } = false;
    public bool RandomizeStockAvailable { get; set; } = false;
    public int OutOfStockChance { get; set; } = 20;

    public double PriceMultiplier { get; set; } = 1.0;
    public double InsurancePriceCoef { get; set; } = 1.0;
    public double RepairQuality { get; set; } = 1.0;

    public int TraderRefreshMin { get; set; } = 3600;
    public int TraderRefreshMax { get; set; } = 7200;
}