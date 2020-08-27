using System.Text.Json.Serialization;

namespace TpLink.Api.Models
{
    public class WirelessModel
    {
        // todo: add json converter. string off = false, string on = true
        public string Enable { get; set; }
        public string SSID { get; set; }
        public string Hidden { get; set; }
        public string Encryption { get; set; }
        [JsonPropertyName("psk_version")]
        public string PskVersion { get; set; }
        [JsonPropertyName("psk_cipher")]
        public string PskCipher { get; set; }
        [JsonPropertyName("psk_key")]
        public string PskKey { get; set; }
        [JsonPropertyName("wep_mode")]
        public string WepMode { get; set; }
        [JsonPropertyName("wep_select")]
        public string WepSelect { get; set; }
        [JsonPropertyName("wep_key1")]
        public string WepKey1 { get; set; }
        [JsonPropertyName("wep_key2")]
        public string WepKey2 { get; set; }
        [JsonPropertyName("wep_key3")]
        public string WepKey3 { get; set; }
        [JsonPropertyName("wep_key4")]
        public string WebKey4 { get; set; }
        [JsonPropertyName("wep_type1")]
        public string WepType1 { get; set; }
        [JsonPropertyName("wep_type2")]
        public string WepType2 { get; set; }
        [JsonPropertyName("wep_type3")]
        public string WepType3 { get; set; }
        [JsonPropertyName("wep_type4")]
        public string WepType4 { get; set; }
        [JsonPropertyName("wep_format1")]
        public string WepFormat1 { get; set; }
        [JsonPropertyName("wep_format2")]
        public string WepFormat2 { get; set; }
        [JsonPropertyName("wep_format3")]
        public string WepFormat3 { get; set; }
        [JsonPropertyName("wep_format4")]
        public string WepFormat4 { get; set; }
        public string Hwmode { get; set; }
        public string Htmode { get; set; }
        public string Channel { get; set; }
        public string Txpower { get; set; }
        [JsonPropertyName("disabled")]
        public string Disabled { get; set; }
        [JsonPropertyName("wireless_2g_disabled")]
        public string Wireless2GDisabled { get; set; }
        [JsonPropertyName("wireless_2g_disabled_all")]
        public string Wireless2GDisabledAll { get; set; }
    }
}

// todo: data type
// jsonproperty-name