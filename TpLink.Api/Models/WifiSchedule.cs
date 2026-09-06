using System.Text.Json.Serialization;
using TpLink.Api.Converters;

namespace TpLink.Api.Models;

[Flags]
public enum Days : byte
{
    Sunday = 1,
    Monday = 2,
    Tuesday = 4,
    Wednesday = 8,
    Thursday = 16,
    Friday = 32,
    Saturday = 64,
}

/// <summary>
/// A Wi-Fi schedule rule. Serializes to the payload the device expects:
/// <c>{"stime":"20","etime":"23","days":"127","week_sun":"1",...,"enable":"on"}</c>, where the
/// <c>week_*</c> flags are derived from <see cref="Days"/>.
/// </summary>
public class WifiSchedule
{
    /// <summary>Start hour, 0 to 24.</summary>
    [JsonPropertyName("stime")]
    [JsonConverter(typeof(IntToString))]
    public int StartTime { get; set; }

    /// <summary>End hour, 0 to 24; must be later than <see cref="StartTime"/>.</summary>
    [JsonPropertyName("etime")]
    [JsonConverter(typeof(IntToString))]
    public int EndTime { get; set; }

    [JsonPropertyName("days")]
    [JsonConverter(typeof(DaysEnumToCustomString))]
    public Days Days { get; set; }

    [JsonPropertyName("enable")]
    [JsonConverter(typeof(StringBoolConverter))]
    public bool Enable { get; set; }

    [JsonPropertyName("week_mon")]
    [JsonConverter(typeof(BoolToBitConvert))]
    public bool Monday => Days.HasFlag(Days.Monday);

    [JsonPropertyName("week_tues")]
    [JsonConverter(typeof(BoolToBitConvert))]
    public bool Tuesday => Days.HasFlag(Days.Tuesday);

    [JsonPropertyName("week_wed")]
    [JsonConverter(typeof(BoolToBitConvert))]
    public bool Wednesday => Days.HasFlag(Days.Wednesday);

    [JsonPropertyName("week_thur")]
    [JsonConverter(typeof(BoolToBitConvert))]
    public bool Thursday => Days.HasFlag(Days.Thursday);

    [JsonPropertyName("week_fri")]
    [JsonConverter(typeof(BoolToBitConvert))]
    public bool Friday => Days.HasFlag(Days.Friday);

    [JsonPropertyName("week_sat")]
    [JsonConverter(typeof(BoolToBitConvert))]
    public bool Saturday => Days.HasFlag(Days.Saturday);

    [JsonPropertyName("week_sun")]
    [JsonConverter(typeof(BoolToBitConvert))]
    public bool Sunday => Days.HasFlag(Days.Sunday);
}
