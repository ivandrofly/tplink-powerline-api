using System.Text.Json.Serialization;

namespace TpLink.Models
{
    public class Guest5G : IGuestNetwork
    {
        [JsonPropertyName("guest_5g_enable")]
        public string Enabled { get; set; }
        [JsonPropertyName("guest_5g_disabled")]
        public string Disabled { get; set; }
        [JsonPropertyName("guest_5g_hidden")]
        public string Hidden { get; set; }
        [JsonPropertyName("guest_5g_ssid")]
        public string SSID { get; set; }
        [JsonPropertyName("guest_5g_psk_key")]
        public string PSKKey { get; set; }
        [JsonPropertyName("guest_5g_encryption")]
        public string Encryption { get; set; }
    }
}
