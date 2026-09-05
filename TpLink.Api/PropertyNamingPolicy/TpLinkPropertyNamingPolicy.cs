using System.Text.Json;

namespace TpLink.Api.PropertyNamingPolicy
{
    /// <summary>
    /// Lower-cases property names that carry no <c>JsonPropertyName</c> attribute (<c>SSID</c> becomes <c>ssid</c>,
    /// <c>Enable</c> becomes <c>enable</c>), which is how the adapter names every field. Registered on the shared
    /// serializer options, it drives both response matching and the form fields emitted from a model.
    /// </summary>
    /// <remarks>
    /// An earlier version tried to insert underscores at case boundaries ("PskKey" to "psk_key") but was never
    /// registered; fields that need underscores carry an explicit <c>JsonPropertyName</c> instead.
    /// </remarks>
    public class TpLinkPropertyNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name.ToLowerInvariant();
    }
}
