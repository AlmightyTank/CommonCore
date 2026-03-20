using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Utils;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public sealed class StaticAmmoHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db)
{
    private readonly CommonCoreDb _db = db;

    public void Process(ItemCreationRequest request)
    {
        if (!request.AddToStaticAmmo)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.NewId))
        {
            debugLogHelper.LogError("StaticAmmoHelper", $"NewId missing.");
            return;
        }

        try
        {
            var caliber = ResolveCaliber(request);
            if (string.IsNullOrWhiteSpace(caliber))
            {
                debugLogHelper.LogError("StaticAmmoHelper", $"Item {request.NewId} has no Caliber property, cannot add to static ammo.");
                return;
            }

            var probability = request.StaticAmmoProbability ?? 0;

            debugLogHelper.LogService("StaticAmmoHelper", $"Adding ammo {request.NewId} to all location static ammo pools");
            debugLogHelper.LogService("StaticAmmoHelper", $"  Caliber: {caliber}");
            debugLogHelper.LogService("StaticAmmoHelper", $"  Probability: {probability}");

            var locationsUpdated = 0;

            foreach (var (locationId, location) in _db.Locations)
            {
                if (location.StaticAmmo == null)
                {
                    continue;
                }

                try
                {
                    var ammoList = location.StaticAmmo.TryGetValue(caliber, out var existingDetails)
                        ? existingDetails.ToList()
                        : [];

                    if (ammoList.Any(a => string.Equals(a.Tpl, request.NewId, StringComparison.OrdinalIgnoreCase)))
                    {
                        debugLogHelper.LogService("StaticAmmoHelper", $"Ammo {request.NewId} already exists in {caliber} for {locationId}, skipping.");
                        continue;
                    }

                    ammoList.Add(new StaticAmmoDetails
                    {
                        Tpl = request.NewId,
                        RelativeProbability = probability
                    });

                    location.StaticAmmo[caliber] = ammoList;

                    debugLogHelper.LogService("StaticAmmoHelper", $"Added {request.NewId} to {caliber} in {locationId}");
                    locationsUpdated++;
                }
                catch (Exception ex)
                {
                    debugLogHelper.LogError("StaticAmmoHelper", $"Error adding ammo to location {locationId}: {ex.Message}");
                    debugLogHelper.LogService("StaticAmmoHelper", $"Stack trace: {ex.StackTrace}");
                }
            }

            debugLogHelper.LogService("StaticAmmoHelper", $"Added {request.NewId} to static ammo pools in {locationsUpdated} locations.");
        }
        catch (Exception ex)
        {
            debugLogHelper.LogError("StaticAmmoHelper", $"[StaticAmmo] Error adding ammo to location static ammo: {ex.Message}");
            debugLogHelper.LogService("StaticAmmoHelper", $"Stack trace: {ex.StackTrace}");
        }
    }

    private string? ResolveCaliber(ItemCreationRequest request)
    {
        var overrideCaliber = request.OverrideProperties?.Caliber;
        if (!string.IsNullOrWhiteSpace(overrideCaliber))
        {
            return overrideCaliber;
        }

        if (_db.Items.TryGetValue(new MongoId(request.NewId), out var createdItem))
        {
            return createdItem.Properties?.Caliber;
        }

        return null;
    }
}