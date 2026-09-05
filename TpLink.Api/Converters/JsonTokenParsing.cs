using System;
using System.Globalization;
using System.Text.Json;

namespace TpLink.Api.Converters
{
    /// <summary>
    /// Shared, token-type-aware parsing for the converters. The adapter usually sends every value as a string
    /// ("on", "1", "20"), but the converters also accept real JSON booleans, numbers and null so a firmware
    /// change in that direction does not throw.
    /// </summary>
    internal static class JsonTokenParsing
    {
        /// <summary>true for "on", "1", "true", "yes" (case-insensitive) and non-zero numbers; false otherwise, including null.</summary>
        public static bool ReadBool(ref Utf8JsonReader reader, string expected) =>
            reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Null => false,
                JsonTokenType.Number => reader.GetDouble() != 0,
                JsonTokenType.String => ParseBool(reader.GetString()),
                _ => throw new JsonException($"Cannot convert a {reader.TokenType} token to a bool; expected {expected}."),
            };

        public static bool ParseBool(string value) =>
            value?.Trim().ToLowerInvariant() is "on" or "1" or "true" or "yes";

        /// <summary>Numbers as-is, numeric strings parsed invariantly, booleans as 1/0, null as 0.</summary>
        public static int ReadInt(ref Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    return reader.GetInt32();
                case JsonTokenType.True:
                    return 1;
                case JsonTokenType.False:
                case JsonTokenType.Null:
                    return 0;
                case JsonTokenType.String:
                    var text = reader.GetString();
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                    {
                        return number;
                    }

                    throw new JsonException($"Cannot convert \"{text}\" to an int.");
                default:
                    throw new JsonException($"Cannot convert a {reader.TokenType} token to an int.");
            }
        }
    }
}
