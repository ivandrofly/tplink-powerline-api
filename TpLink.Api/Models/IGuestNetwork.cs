namespace TpLink.Models
{
    public interface IGuestNetwork
    {
        string Enabled { get; set; }
        string Disabled { get; set; }
        string Hidden { get; set; }
        string SSID { get; set; }
        string PSKKey { get; set; }
        string Encryption { get; set; }
    }
}
