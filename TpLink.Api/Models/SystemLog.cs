namespace TpLink.Api.Models
{
    public class SystemLog
    {
        // todo: add value converter for this. for now i will use it as string
        //public TimeSpan Time { get; set; }
        public string Time { get; set; }
        public string Type { get; set; }
        public string Level { get; set; }

        // case incensitive is being used, this is no needed, but was required somehow
        //[JsonPropertyName("content")] 
        public string Content { get; set; }

        public override string ToString() => $"time: {Time}, type: {Type}, level: {Level}, content: {Content}";
    }
}
