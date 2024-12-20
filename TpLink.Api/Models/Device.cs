using System.Text.Json.Serialization;

namespace TpLink.Api.Models
{
    public class Device
    {
        [JsonPropertyName("device_mac")]
        public string Mac { get; set; }

        [JsonPropertyName("device_password")]
        public string Password { get; set; }

        [JsonPropertyName("rx_rate")]
        public string RXRate { get; set; }

        [JsonPropertyName("tx_rate")]
        public string TXRate { get; set; }

        public string Status { get; set; }

        public override string ToString() => $"tx-rate: {TXRate}, rx-rate: {RXRate}";
    }
}
