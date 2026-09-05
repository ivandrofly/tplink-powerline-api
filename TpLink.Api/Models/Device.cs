using System.Text.Json.Serialization;

namespace TpLink.Api.Models;

/// <summary>A powerline peer (admin/powerline?form=plc_device).</summary>
public class Device
{
    [JsonPropertyName("device_mac")]
    public string? Mac { get; set; }

    /// <summary>The powerline network key. Never log it.</summary>
    [JsonPropertyName("device_password")]
    public string? Password { get; set; }

    [JsonPropertyName("rx_rate")]
    public string? RXRate { get; set; }

    [JsonPropertyName("tx_rate")]
    public string? TXRate { get; set; }

    public string? Status { get; set; }

    public override string ToString() => $"tx-rate: {TXRate}, rx-rate: {RXRate}";
}
