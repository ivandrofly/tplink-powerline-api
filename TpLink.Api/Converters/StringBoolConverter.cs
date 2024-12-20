using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TpLink.Api.Converters
{
    /// <summary>
    /// Writes string to boolean true=on and false=off
    /// </summary>
    public class StringBoolConverter : JsonConverter<bool>
    {
        /// <summary>
        /// Reads and converts JSON data to a boolean value.
        /// Expects either a boolean or a string ("on" or "off") representation.
        /// </summary>
        /// <param name="reader">The UTF-8 JSON reader to read input data from.</param>
        /// <param name="typeToConvert">The type to convert the JSON data to, expected to be a boolean.</param>
        /// <param name="options">Options for deserialization.</param>
        /// <returns>A boolean value that is either true or false based on the input data.</returns>
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

        /// <summary>
        /// Writes a boolean value as a JSON string, converting true to "on" and false to "off".
        /// </summary>
        /// <param name="writer">The UTF-8 JSON writer to write the string value to.</param>
        /// <param name="value">The boolean value to be converted and written.</param>
        /// <param name="options">Options for serialization.</param>
        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            //writer.WriteString("none", DateTime.Now);
            writer.WriteStringValue(value ? "on" : "off");
        }
    }
}
