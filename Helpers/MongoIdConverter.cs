using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace CommonLibExtended.Helpers;

public class MongoIdConverter : JsonConverter<MongoId>
{
    public override MongoId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return string.IsNullOrWhiteSpace(value) ? new MongoId() : new MongoId(value);
    }

    public override void Write(Utf8JsonWriter writer, MongoId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}