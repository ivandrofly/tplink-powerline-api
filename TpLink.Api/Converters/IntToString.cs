using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TpLink.Api.Converters
{
    /// <summary>
    /// Maps the device's numeric strings ("20") to <see cref="int"/>. Reading also accepts JSON numbers, booleans
    /// (1/0) and null (0); a non-numeric string throws <see cref="JsonException"/>. Writing always emits a string.
    /// </summary>
    public class IntToString : JsonConverter<int>
    {
        public override bool HandleNull => true;

        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            JsonTokenParsing.ReadInt(ref reader);

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
    }
}
