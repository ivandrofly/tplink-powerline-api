using System.Text.Json;

namespace TpLink.Api.PropertyNamingPolicy;

/// <summary>
/// <c>PskKey</c> becomes <c>{prefix}psk_key</c>. Lets one <see cref="Models.GuestNetwork"/> model read both the
/// <c>guest_2g_*</c> and the <c>guest_5g_*</c> payloads; properties with an explicit <c>JsonPropertyName</c> are untouched.
/// </summary>
public sealed class PrefixedSnakeCaseNamingPolicy(string prefix) : JsonNamingPolicy
{
    public override string ConvertName(string name) => prefix + SnakeCaseLower.ConvertName(name);
}
