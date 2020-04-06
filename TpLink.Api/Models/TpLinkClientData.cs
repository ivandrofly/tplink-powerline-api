using System.Text.Json.Serialization;

namespace TpLink.Models
{
    public class TpLinkClientData : TpLinkData<Client>
    {
        [JsonPropertyName("max_rules")]
        public string MaxRules { get; set; }
    }
}
