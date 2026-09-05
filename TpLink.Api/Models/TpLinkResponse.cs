using System.Text.Json.Serialization;

namespace TpLink.Api.Models;

/// <summary>
/// The envelope every endpoint answers with. <see cref="Success"/> is <c>false</c> and <see cref="Data"/> is
/// <c>null</c> when the device rejects the request (typically while its web manager is open in a browser).
/// </summary>
public class TpLinkResponse<TData>
{
    [JsonPropertyName("timeout")]
    public bool Timeout { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public TData? Data { get; set; }
}
