using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TpLink.Api.Models;

namespace TpLink.Api.Converters
{
    /// <summary>
    /// A JSON converter for the <see cref="Days"/> enum, which provides custom serialization and deserialization behavior.
    /// </summary>
    /// <remarks>
    /// The converter converts the <see cref="Days"/> enum to and from its byte representation as a string.
    /// During serialization, the enum value is represented as a string containing its underlying byte value.
    /// During deserialization, the string is parsed back into the corresponding <see cref="Days"/> enum value.
    /// </remarks>
    /// <example>
    /// The serialization and deserialization processes use the byte value of the <see cref="Days"/> enum in string format.
    /// This behavior enables compatibility with APIs or storage mechanisms expecting the custom string format for representing days.
    /// </example>
    public class DaysEnumToCustomString : JsonConverter<Days>
    {
        /// <summary>
        /// Reads and converts the JSON-encoded string representation of a <see cref="Days"/> enum into its corresponding enum value.
        /// </summary>
        /// <param name="reader">The UTF-8 JSON reader used to read the JSON string.</param>
        /// <param name="typeToConvert">The type of the object to convert, which is expected to be of <see cref="Days"/>.</param>
        /// <param name="options">Options that are passed to the converter, unused in this implementation.</param>
        /// <returns>The <see cref="Days"/> enum value that corresponds to the JSON string representation.</returns>
        public override Days Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return (Days)Enum.Parse(typeof(Days), reader.GetString());
        }

        /// <summary>
        /// Writes the JSON string representation of the <see cref="Days"/> enum using its underlying byte value as a string.
        /// </summary>
        /// <param name="writer">The UTF-8 JSON writer used to write the JSON string.</param>
        /// <param name="value">The <see cref="Days"/> enum value to be written.</param>
        /// <param name="options">Options that are passed to the converter, unused in this implementation.</param>
        public override void Write(Utf8JsonWriter writer, Days value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(((byte)value).ToString());
        }
    }
}
