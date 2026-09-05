namespace TpLink.Api.Models;

/// <summary>
/// Guest network settings for one band (admin/guest?form=guest_2g|guest_5g). The device prefixes every field
/// with <c>guest_2g_</c> or <c>guest_5g_</c>; the client reads both with a per-band naming policy, so the
/// property names here are the bare field names (<see cref="PskKey"/> is <c>guest_2g_psk_key</c>).
/// </summary>
public class GuestNetwork
{
    public string? Enable { get; set; }
    public string? Disabled { get; set; }
    public string? Hidden { get; set; }
    public string? Ssid { get; set; }
    public string? PskKey { get; set; }
    public string? Encryption { get; set; }
}
