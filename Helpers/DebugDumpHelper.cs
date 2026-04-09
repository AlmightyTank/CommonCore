using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommonLibExtended.Helpers;

public static class DebugDumpHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string DumpItems(object obj)
    {
        try
        {
            return JsonSerializer.Serialize(obj, Options);
        }
        catch (Exception ex)
        {
            return $"<Failed to serialize: {ex.Message}>";
        }
    }
}  