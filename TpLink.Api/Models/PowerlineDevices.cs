using System.Text.Json.Serialization;

namespace TpLink.Models
{
    public class PowerlineDevices
    {
        [JsonPropertyName("device_mac")]
        public string DeviceMac { get; set; }
        [JsonPropertyName("device_password")]
        public string DevicePassword { get; set; }
        [JsonPropertyName("rx_rate")]
        public string RXRate { get; set; }
        [JsonPropertyName("tx_rate")]
        public string TXRate { get; set; }
        public string Status { get; set; }

        public override string ToString() => $"tx-rate: {TXRate}, rx-rate: {RXRate}";
    }
}
