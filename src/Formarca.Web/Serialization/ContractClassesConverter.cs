using System.Text.Json;
using System.Text.Json.Serialization;

namespace Formarca.Web.Serialization;

public sealed class ContractClassesConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return string.Empty;
            case JsonTokenType.String:
                return reader.GetString() ?? string.Empty;
            case JsonTokenType.StartArray:
                var parts = new List<string>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    var part = reader.TokenType switch
                    {
                        JsonTokenType.Number => reader.TryGetInt32(out var number)
                            ? number.ToString()
                            : reader.GetDecimal().ToString("0.##"),
                        JsonTokenType.String => reader.GetString(),
                        _ => null
                    };
                    if (!string.IsNullOrWhiteSpace(part))
                    {
                        parts.Add(part.Trim());
                    }
                }

                return string.Join(", ", parts);
            default:
                throw new JsonException("Classes must be a string or an array of numbers.");
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
