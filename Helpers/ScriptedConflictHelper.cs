using CommonCore.Models;
using CommonCore.Items.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace CommonCore.Helpers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public sealed class ScriptedConflictHelper(
    CompatibilityService compatibilityService,
    CoreDebugLogHelper debugLogHelper)
{
    private readonly CompatibilityService _compatibilityService = compatibilityService;
    private readonly CoreDebugLogHelper _debugLogHelper = debugLogHelper;

    public void Process(CommonCoreItemRequest request)
    {
        if (request == null)
        {
            _debugLogHelper.LogError("ScriptedConflictHelper", "Request was null.");
            return;
        }

        if (request.Config == null)
        {
            _debugLogHelper.LogError("ScriptedConflictHelper", $"Config was null for item '{request.ItemId}'.");
            return;
        }

        if (string.IsNullOrWhiteSpace(request.ItemId))
        {
            _debugLogHelper.LogService("ScriptedConflictHelper", "Skipped scripted conflicts because request.ItemId was empty.");
            return;
        }

        var infos = request.Config.ScriptedConflictingInfos;
        if (infos == null || infos.Length == 0)
        {
            _debugLogHelper.LogService("ScriptedConflictHelper", $"No scripted conflicts for item '{request.ItemId}'.");
            return;
        }

        var validConflicts = infos
            .Where(x => x != null)
            .ToArray();

        if (validConflicts.Length == 0)
        {
            _debugLogHelper.LogService("ScriptedConflictHelper", $"Scripted conflicts were present but empty after filtering for item '{request.ItemId}'.");
            return;
        }

        _compatibilityService.AddScriptedConflicts(request.ItemId, validConflicts);

        _debugLogHelper.LogService(
            "ScriptedConflictHelper",
            $"Added {validConflicts.Length} scripted conflicts for item '{request.ItemId}'.");
    }
}