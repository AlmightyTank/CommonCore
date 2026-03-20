namespace CommonCore.Traders.Service.Sub;

using CommonCore.Traders.Models;
using SPTarkov.DI.Annotations;

[Injectable]
public sealed class TraderSettingsApplyService
{
    public void Apply(TraderLoadContext context)
    {
        var traderBase = context.TraderBase;
        var settings = context.Settings;

        traderBase.UnlockedByDefault = settings.UnlockedByDefault;

        if (traderBase.LoyaltyLevels != null && traderBase.LoyaltyLevels.Count > 0)
        {
            traderBase.LoyaltyLevels[0].MinLevel = settings.MinLevel;
        }

        if (traderBase.LoyaltyLevels != null)
        {
            foreach (var loyaltyLevel in traderBase.LoyaltyLevels)
            {
                var property = loyaltyLevel.GetType().GetProperty("InsurancePriceCoefficient");
                if (property is { CanWrite: true })
                {
                    var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    var convertedValue = Convert.ChangeType(settings.InsurancePriceCoef, targetType);
                    property.SetValue(loyaltyLevel, convertedValue);
                }
            }
        }

        if (traderBase.Insurance != null && traderBase.Insurance.ExtensionData != null)
        {
            traderBase.Insurance.ExtensionData["insurance_price_coef"] = settings.InsurancePriceCoef;
        }

        if (traderBase.Repair != null)
        {
            traderBase.Repair.Quality = settings.RepairQuality;
        }
    }
}