using System.Text.Json.Serialization;

namespace TpLink.Api.Models;

/// <summary>The wireless client list is the one response with an extra top-level field.</summary>
public class TpLinkClientData : TpLinkResponse<List<Client>>
{
    [JsonPropertyName("max_rules")]
    public string? MaxRules { get; set; }
}
