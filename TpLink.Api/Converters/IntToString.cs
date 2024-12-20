using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TpLink.Api.Converters
{
    /// <summary>
    /// A custom JSON converter for handling conversion between integers and their string representations in JSON data.
    /// </summary>
    /// <remarks>
    /// This converter is designed to serialize integer values into a string format and deserialize string representations of integers back into their integer form.
    /// It is particularly useful in scenarios where the JSON data represents numeric values as strings.
    /// </remarks>
    /// <typeparam name="T">The type of the value being converted, constrained to <see cref="int"/> by default implementation.</typeparam>
    public class IntToString : JsonConverter<int>
    {
        /// <summary>
        /// Reads the string representation of an integer from the JSON input and converts it to an integer value.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/> instance used to read the JSON data.</param>
        /// <param name="typeToConvert">The <see cref="Type"/> of the value being converted.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> that provide configurations for deserialization.</param>
        /// <returns>The integer value converted from its string representation in the JSON data.</returns>
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Convert.ToInt32(reader.GetString());
        }

        /// <summary>
        /// Writes the integer value as a string representation to the JSON output.
        /// </summary>
        /// <param name="writer">The <see cref="Utf8JsonWriter"/> instance used to write the JSON data.</param>
        /// <param name="value">The integer value to be serialized into a string format.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> that provide configurations for serialization.</param>
        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
