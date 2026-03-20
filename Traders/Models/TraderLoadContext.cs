namespace CommonCore.Traders.Models;

using CommonCore.Core;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Servers;

public sealed class TraderLoadContext
{
    public required string ModPath { get; init; }
    public required ITraderDefinition Definition { get; init; }
    public required TraderBase TraderBase { get; set; }
    public required TraderAssort Assort { get; set; }
    public required TraderRuntimeSettings Settings { get; set; }
}