namespace CommonCore.Traders.Models;

using SPTarkov.Server.Core.Models.Eft.Common.Tables;

public interface ITraderDefinition
{
    string TraderId { get; }
    string BaseFilePath { get; }
    string AssortFilePath { get; }
    string AvatarFilePath { get; }
    string DefaultLocaleName { get; }
    string DefaultLocaleDescription { get; }
    string ConfigFilePath { get; }

    /// <summary>
    /// Optional trader-specific customization point.
    /// Runs after base data/config load and sanity checks,
    /// before the shared CommonCore services apply runtime changes.
    /// </summary>
    void Configure(TraderLoadContext context);
}