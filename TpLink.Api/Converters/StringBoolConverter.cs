﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TpLink.Api.Converters
{
    /// <summary>
    /// Writes string to boolean true=on and false=off
    /// </summary>
    public class StringBoolConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                return reader.GetBoolean();
            }
            catch (InvalidOperationException)
            {
                string value = reader.GetString();
                // expected values: on / off
                return value.Equals("on", StringComparison.OrdinalIgnoreCase);
            }
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            //writer.WriteString("none", DateTime.Now);
            writer.WriteStringValue(value ? "on" : "off");
        }
    }
}
