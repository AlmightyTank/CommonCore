namespace CommonCore.Traders.Service.Sub;

using CommonCore.Helpers;
using CommonCore.Traders.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Routers;
using Path = Path;

[Injectable]
public sealed class TraderImageService(ImageRouter imageRouter, CoreDebugLogHelper debugLogService)
{
    public void Apply(TraderLoadContext context)
    {
        var traderImagePath = Path.Combine(context.ModPath, context.Definition.AvatarFilePath);

        var avatarRoute = context.TraderBase.Avatar ?? string.Empty;
        avatarRoute = avatarRoute
            .Replace(".png", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".jpg", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".jpeg", string.Empty, StringComparison.OrdinalIgnoreCase);

        debugLogService.LogService("Image",
            $"Avatar route mapped: {avatarRoute}");

        if (string.IsNullOrWhiteSpace(avatarRoute))
        {
            return;
        }

        imageRouter.AddRoute(avatarRoute, traderImagePath);
    }
}