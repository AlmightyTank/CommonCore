using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public sealed class ScriptedConflictHelper(
    CompatibilityService compatibilityService)
{
    public void Process(ItemCreationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewId))
        {
            LogHelper.Log("[ScriptedConflictHelper] Skipped scripted conflicts because request.NewId was empty.");
            return;
        }

        if (request.ScriptedConflictingInfos == null || request.ScriptedConflictingInfos.Length == 0)
        {
            LogHelper.LogDebug($"[ScriptedConflictHelper] No scripted conflicts for item '{request.NewId}'.");
            return;
        }

        var validConflicts = request.ScriptedConflictingInfos
            .Where(x => x != null)
            .Distinct()
            .ToArray();

        if (validConflicts.Length == 0)
        {
            LogHelper.LogDebug($"[ScriptedConflictHelper] Scripted conflicts were present but empty after filtering for item '{request.NewId}'.");
            return;
        }

        compatibilityService.AddScriptedConflicts(request.NewId, request.ScriptedConflictingInfos);

        LogHelper.LogDebug(
            $"[ScriptedConflictHelper] Added {validConflicts.Length} scripted conflicts for item '{request.NewId}'.");
    }
}