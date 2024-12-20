using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TpLink.Api.Models
{
    public class TpLinkClientData : TpLinkResponse<List<Client>>, IResponseModel<List<Client>>
    {
        [JsonPropertyName("max_rules")]
        public string MaxRules { get; set; }
    }
}
