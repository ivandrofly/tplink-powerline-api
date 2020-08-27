using System.Collections.Generic;
using System.Text.Json.Serialization;
using TpLink.Api.Models;

namespace TpLink.Models
{
    public class TpLinkClientData : TpLinkData<List<Client>>, IResponseModel<List<Client>>
    {
        [JsonPropertyName("max_rules")]
        public string MaxRules { get; set; }
    }
}
