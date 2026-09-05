using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using TpLink.Api.Models;

namespace TpLink.Api.Converters
{
    /// <summary>
    /// Maps the device's day bitmask, sent as a numeric string ("127" = every day), to the <see cref="Days"/> flags
    /// enum. Reading also accepts JSON numbers, flag names ("Monday, Tuesday") and null (no days); anything else
    /// throws <see cref="JsonException"/>. Writing always emits the byte value as a string.
    /// </summary>
    public class DaysEnumToCustomString : JsonConverter<Days>
    {
        public override bool HandleNull => true;

        public override Days Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return 0;
                case JsonTokenType.Number:
                    return (Days)reader.GetByte();
                case JsonTokenType.String:
                    var text = reader.GetString();
                    if (byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bits))
                    {
                        return (Days)bits;
                    }

                    if (Enum.TryParse<Days>(text, ignoreCase: true, out var named))
                    {
                        return named;
                    }

                    throw new JsonException($"Cannot convert \"{text}\" to {nameof(Days)}; expected a bitmask such as \"127\".");
                default:
                    throw new JsonException($"Cannot convert a {reader.TokenType} token to {nameof(Days)}.");
            }
        }

        public override void Write(Utf8JsonWriter writer, Days value, JsonSerializerOptions options) =>
            writer.WriteStringValue(((byte)value).ToString(CultureInfo.InvariantCulture));
    }
}
