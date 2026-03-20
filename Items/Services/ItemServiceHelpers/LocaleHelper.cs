using CommonCore.Helpers;
using CommonCore.Items.Models;
using CommonCore.Items.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable(InjectionType.Singleton)]
public class LocaleHelper(
    CoreDebugLogHelper debugLogHelper,
    ContentService contentService
)
{
    public void Process(ItemCreationRequest request)
    {
        if (request.Locales == null || request.Locales.Count == 0)
        {
            return;
        }

        contentService.AddLocales(request.Locales);
        debugLogHelper.LogService("LocaleHelper", $"Added additional locales for {request.NewId}");
    }
}