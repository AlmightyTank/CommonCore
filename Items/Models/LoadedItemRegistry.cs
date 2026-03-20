using SPTarkov.DI.Annotations;

namespace CommonCore.Items.Models;

[Injectable(InjectionType.Singleton)]
public class LoadedItemRegistry
{
    private readonly HashSet<string> _loadedItems = new(StringComparer.OrdinalIgnoreCase);

    public bool Contains(string itemId) => _loadedItems.Contains(itemId);

    public bool Add(string itemId) => _loadedItems.Add(itemId);

    public void Clear() => _loadedItems.Clear();

    public IReadOnlyCollection<string> GetAll() => _loadedItems;
}
