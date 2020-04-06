namespace TpLink.Api.Models
{
    public class WirelessModel
    {
        public bool Enable { get; set; }
        public string SSID { get; set; }
        public bool Hidden { get; set; }
        public string Encryption { get; set; }
        public string PskVersion { get; set; }
        public string PskCipher { get; set; }
        public string PskKey { get; set; }
        public string WepMode { get; set; }
        public string WepSelect { get; set; }
        public string WepType1 { get; set; }
        public string WepKey1 { get; set; }
        public string WepKey2 { get; set; }
        public string WepKey3 { get; set; }
        public string WebKey4 { get; set; }
        public string WepType2 { get; set; }
        public string WepType3 { get; set; }
        public string WepType4 { get; set; }
        public string WepFormat1 { get; set; }
        public string WepFormat2 { get; set; }
        public string WepFormat3 { get; set; }
        public string WepFormat4 { get; set; }
        public string HwMode { get; set; }
        public string HTMode { get; set; }
        public string Channel { get; set; }
        public string TXPower { get; set; }
        public bool Disabled { get; set; }
        public string Wireless2GDisabled { get; set; }
        public string Wireless2GDisabledAll { get; set; }
    }
}

// todo: data type
// jsonproperty-name