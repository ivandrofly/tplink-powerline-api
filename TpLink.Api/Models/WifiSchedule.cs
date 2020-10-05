using System;
using System.Text.Json.Serialization;
using System.Threading;
using TpLink.Api.Converters;

namespace TpLink.Api.Models
{
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
    /// // {"stime":"20","etime":"23","days":"127","week_sun":"1","week_mon":"1","week_tues":"1","week_wed":"1","week_thur":"1","week_fri":"1","week_sat":"1","enable":"on"}
    /// </summary>
    public class WifiSchedule
    {
        /// <summary>
        /// Start-time from 0 to 24
        /// </summary>
        [JsonPropertyName("stime")]
        [JsonConverter(typeof(IntToString))]
        public int StartTime { get; set; }

        /// <summary>
        /// End-time from 0 to 24.
        /// </summary>
        [JsonPropertyName("etime")]
        [JsonConverter(typeof(IntToString))]
        public int EndTime { get; set; }

        [JsonPropertyName("days")]
        [JsonConverter(typeof(DaysEnumToCustomString))]
        public Days Days { get; set; }

        [JsonPropertyName("enable")]
        [JsonConverter(typeof(StringBoolConverter))]
        public bool Enable { get; set; }


        // NOTE THESE PROPERTY SHOULD READ THEIR VALUES FROM THE ENUM DAYS
        [JsonPropertyName("week_mon")]
        [JsonConverter(typeof(BoolToBitConvert))]
        public bool Monday => Days.HasFlag(Days.Monday);

        [JsonPropertyName("week_tues")]
        [JsonConverter(typeof(BoolToBitConvert))]
        public bool Tuesday => (Days.Tuesday & Days) == Days.Tuesday;

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
}
