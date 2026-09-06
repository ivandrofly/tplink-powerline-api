namespace TpLink.Api.Models
{
    public class SystemLog
    {
        // todo: add value converter for this. for now i will use it as string
        //public TimeSpan Time { get; set; }
        public string Time { get; set; }
        public string Type { get; set; }
        public string Level { get; set; }

        // matching is case-insensitive, so no JsonPropertyName is needed here
        public string Content { get; set; }

        public override string ToString() => $"time: {Time}, type: {Type}, level: {Level}, content: {Content}";
    }
}
