namespace CommonLibExtended.Models;

public sealed class CustomTraderUnlockRegistration
{
    public string TraderId { get; set; } = string.Empty;
    public int MinLevel { get; set; } = 1;
    public bool UnlockedByDefault { get; set; }
}