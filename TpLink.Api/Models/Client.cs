using System.Text.Json.Serialization;

namespace TpLink.Api.Models;

/// <summary>
/// A wireless client connected to the adapter (admin/wireless?form=statistics). Every field is a string as sent
/// by the device; a client without a DHCP lease reports an empty <see cref="IP"/>.
/// </summary>
public class Client
{
    public string? Mac { get; set; }
    public string? Type { get; set; }
    public string? Encryption { get; set; }

    [JsonPropertyName("rxpkts")]
    public string? ReceivedPackets { get; set; }

    [JsonPropertyName("txpkts")]
    public string? SentPackets { get; set; }

    public string? IP { get; set; }

    [JsonPropertyName("devName")]
    public string? DeviceName { get; set; }

    public override string ToString() => $"Mac: {Mac}, type: {Type}, encryption: {Encryption}, ip: {IP} received-packets: {ReceivedPackets}, sent-packets: {SentPackets}";
}
