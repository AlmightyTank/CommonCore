using CommonCore.Core;
using CommonCore.Helpers;
using CommonCore.Items.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using System;
using System.Collections.Generic;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;

namespace CommonCore.Items.Services.ItemServiceHelpers;

[Injectable]
public class CaliberHelper(
    CoreDebugLogHelper debugLogHelper,
    CommonCoreDb db
)
{
    public void Process(ItemCreationRequest request)
    {
        if (!request.AddCaliberToAllCloneLocations)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.NewId))
        {
            debugLogHelper.LogError("CaliberHelper", $"NewId is null or empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(request.ItemTplToClone))
        {
            debugLogHelper.LogError("CaliberHelper", $"ItemTplToClone missing for {request.NewId}");
            return;
        }

        var items = db.Items;
        try
        {
            foreach (var (itemId, item) in items)
            {
                UpdateItemFilters(item, request.ItemTplToClone, request.NewId, itemId);
            }
            debugLogHelper.LogService("CaliberHelper", $"Processed caliber config for {request.NewId}");
        }
        catch (Exception)
        {
            debugLogHelper.LogError("CaliberHelper", $"Failed processing caliber config for {request.NewId}");
        }
    }

    private void UpdateItemFilters(TemplateItem item, string cloneId, string newId, string itemId)
    {
        if (item.Properties?.Cartridges != null)
        {
            foreach (var cartridge in item.Properties.Cartridges)
            {
                if (cartridge.Properties?.Filters != null)
                {
                    UpdateFilters(cartridge.Properties.Filters, cloneId, newId, itemId);
                }
            }
        }

        if (item.Properties?.Slots != null)
        {
            foreach (var slot in item.Properties.Slots)
            {
                if (slot.Properties?.Filters != null)
                {
                    UpdateFilters(slot.Properties.Filters, cloneId, newId, itemId);
                }
            }
        }

        if (item.Properties?.Chambers != null)
        {
            foreach (var chamber in item.Properties.Chambers)
            {
                if (chamber.Properties?.Filters != null)
                {
                    UpdateFilters(chamber.Properties.Filters, cloneId, newId, itemId);
                }
            }
        }
    }

    private void UpdateFilters(IEnumerable<SlotFilter> filters, string cloneId, string newId, string itemId)
    {
        foreach (var filter in filters)
        {
            if (filter.Filter != null && filter.Filter.Contains(cloneId) && filter.Filter.Add(newId))
            {
                debugLogHelper.LogService("CaliberHelper", $"Added {newId} to filter in {itemId}");
            }
        }
    }
}