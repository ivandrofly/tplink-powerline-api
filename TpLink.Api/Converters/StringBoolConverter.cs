using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TpLink.Api.Converters
{
    /// <summary>
    /// Maps the device's "on"/"off" strings to <see cref="bool"/>. Reading also accepts JSON booleans, numbers,
    /// "1"/"0", "true"/"false" and null (false); writing always emits "on" or "off".
    /// </summary>
    public class StringBoolConverter : JsonConverter<bool>
    {
        public override bool HandleNull => true;

        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            JsonTokenParsing.ReadBool(ref reader, "\"on\" or \"off\"");

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value ? "on" : "off");
    }
}
