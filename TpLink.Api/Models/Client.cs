using System.Text.Json.Serialization;

namespace TpLink.Models
{
    public class Client
    {
        public string Mac { get; set; }
        public string Type { get; set; }
        public string Encryption { get; set; }
        [JsonPropertyName("rxpkts")]
        public string ReceivedPackets { get; set; }
        [JsonPropertyName("txpkts")]
        public string SentPackets { get; set; }
        public string IP { get; set; }
        [JsonPropertyName("devName")]
        public string DeviceName { get; set; }

        public override string ToString() => $"Mac: {Mac}, type: {Type}, encryption: {Encryption}, ip: {IP} received-packets: {ReceivedPackets}, sent-packets: {SentPackets}";
    }
}
