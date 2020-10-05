using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TpLink.Api.Converters
{
    public class BoolToBitConvert : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                return reader.GetBoolean();
            }
            catch (InvalidOperationException) // fail convertion
            {
                string value = reader.GetString();
                return Convert.ToBoolean(value.Equals("1", StringComparison.Ordinal) ? true : false);
            }
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value ? "1" : "0");
        }
    }
}
