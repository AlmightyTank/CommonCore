namespace CommonLibExtended.Traders.Models;

public sealed class CustomTraderPricingRegistration
{
    public string TraderId { get; set; } = string.Empty;
    public CustomTraderSettings Settings { get; set; } = new();
}