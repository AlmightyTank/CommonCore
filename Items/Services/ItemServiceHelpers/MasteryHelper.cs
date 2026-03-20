using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable]
public class MasteryHelper(CoreDebugLogHelper debugLogHelper, CommonCoreDb db)
{
    public void Process(ItemCreationRequest request)
    {
        if (request.Masteries == null)
            return;

        var masteries = request.Masteries.ToList();
        if (masteries.Count == 0)
        {
            debugLogHelper.LogError("MasteryHelper", $"No mastery sections defined for item {request.NewId}");
            return;
        }

        var globals = db.Globals;

        foreach (var mastery in masteries)
        {
            if (string.IsNullOrEmpty(mastery.Name))
            {
                debugLogHelper.LogError("MasteryHelper", $"Mastery section has no name, skipping.");
                continue;
            }

            var existing = globals.Configuration.Mastering
                .FirstOrDefault(m => m.Name.Equals(mastery.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                var templates = existing.Templates.ToList();

                foreach (var template in mastery.Templates)
                {
                    if (string.IsNullOrEmpty(template))
                    {
                        debugLogHelper.LogError("MasteryHelper", $"Invalid template in mastery section, skipping.");
                        continue;
                    }

                    if (!templates.Contains(template))
                    {
                        templates.Add(template);
                        LogHelper.LogDebug($"Added template {template} to mastery '{mastery.Name}'");
                    }
                }

                existing.Templates = templates.ToArray();
                LogHelper.LogDebug($"[Mastery] Updated existing mastery '{mastery.Name}' for {request.NewId}");
            }
            else
            {
                var newMastery = new Mastering
                {
                    Name = mastery.Name,
                    Level2 = mastery.Level2,
                    Level3 = mastery.Level3,
                    Templates = mastery.Templates.ToArray()
                };

                var newMastering = globals.Configuration.Mastering.ToList();
                newMastering.Add(newMastery);
                globals.Configuration.Mastering = newMastering.ToArray();

                LogHelper.LogDebug($"[Mastery] Created new mastery '{mastery.Name}' for {request.NewId}");
            }
        }
    }
}