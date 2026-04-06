using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils.Json;

namespace CommonLibExtended.Helpers;

[Injectable]
public sealed class CollectionHelper
{
    public bool HasAny(ListOrT<string>? source)
    {
        if (source == null)
        {
            return false;
        }

        if (source.IsList)
        {
            return source.List is { Count: > 0 };
        }

        if (source.IsItem)
        {
            return !string.IsNullOrWhiteSpace(source.Item);
        }

        return false;
    }

    public bool ContainsIgnoreCase(ListOrT<string>? source, string value)
    {
        if (source == null || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (source.IsList && source.List != null)
        {
            foreach (var item in source.List)
            {
                if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        if (source.IsItem)
        {
            return string.Equals(source.Item, value, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public bool AddIfNotExistsIgnoreCase(ListOrT<string>? source, string value)
    {
        if (source == null || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (source.IsList)
        {
            if (source.List == null)
            {
                return false;
            }

            foreach (var item in source.List)
            {
                if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            source.List.Add(value);
            return true;
        }

        if (source.IsItem)
        {
            if (string.Equals(source.Item, value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var existingItem = source.Item;

            if (!string.IsNullOrWhiteSpace(existingItem) && source.List != null)
            {
                source.List.Add(existingItem);
            }

            if (source.List != null)
            {
                source.List.Add(value);
                return true;
            }
            else
            {
                return false;
            }
        }

        if (source.List != null)
        {
            source.List.Add(value);
        }
        else
        {
            return false;
        }
        return true;
    }
}