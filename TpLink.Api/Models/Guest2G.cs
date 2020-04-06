using System.Text.Json.Serialization;

namespace TpLink.Models
{
    public class Guest2G : IGuestNetwork
    {
        [JsonPropertyName("guest_2g_enable")]
        public string Enabled { get; set; }
        [JsonPropertyName("guest_2g_disabled")]
        public string Disabled { get; set; }
        [JsonPropertyName("guest_2g_hidden")]
        public string Hidden { get; set; }
        [JsonPropertyName("guest_2g_ssid")]
        public string SSID { get; set; }
        [JsonPropertyName("guest_2g_psk_key")]
        public string PSKKey { get; set; }
        [JsonPropertyName("guest_2g_encryption")]
        public string Encryption { get; set; }
    }
}
