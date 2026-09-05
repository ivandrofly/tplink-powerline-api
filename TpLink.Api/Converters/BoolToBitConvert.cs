using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TpLink.Api.Converters
{
    /// <summary>
    /// Maps the device's "1"/"0" strings to <see cref="bool"/>. Reading also accepts JSON booleans, numbers,
    /// "on"/"off", "true"/"false" and null (false); writing always emits "1" or "0".
    /// </summary>
    public class BoolToBitConvert : JsonConverter<bool>
    {
        public override bool HandleNull => true;

        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            JsonTokenParsing.ReadBool(ref reader, "\"1\" or \"0\"");

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value ? "1" : "0");
    }
}
