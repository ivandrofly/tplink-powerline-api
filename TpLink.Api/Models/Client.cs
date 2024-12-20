using System.Text.Json.Serialization;

namespace TpLink.Api.Models
{
    /// <summary>
    /// Represents a client connected to a TP-Link network.
    /// </summary>
    /// <remarks>
    /// This class encapsulates information about a network client,
    /// including MAC address, type, encryption status, the number
    /// of packets sent and received, IP address, and device name.
    /// Some properties include custom serialization mappings.
    /// </remarks>
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
