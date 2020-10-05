using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TpLink.Api.Models;

namespace TpLink.Api.Converters
{
    public class DaysEnumToCustomString : JsonConverter<Days>
    {
        public override Days Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return (Days)Enum.Parse(typeof(Days), reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, Days value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(((byte)value).ToString());
        }
    }
}
