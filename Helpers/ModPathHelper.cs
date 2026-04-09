using System.Reflection;
using SPTarkov.DI.Annotations;

namespace CommonLibExtended.Helpers;

[Injectable]
public sealed class ModPathHelper
{
    public string GetModRoot(Assembly assembly)
    {
        var current = new DirectoryInfo(Path.GetDirectoryName(assembly.Location)!);

        while (current != null)
        {
            var dbPath = Path.Combine(current.FullName, "db");
            var packagePath = Path.Combine(current.FullName, "package.json");

            if (Directory.Exists(dbPath) || File.Exists(packagePath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not resolve mod root for assembly {assembly.FullName}");
    }

    public string GetFullPath(Assembly assembly, string relativePath)
    {
        return Path.GetFullPath(Path.Combine(GetModRoot(assembly), relativePath));
    }
}