using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TpLink.Models
{
    public class TpLinkData<TData>
    {
        public bool Timeout { get; set; }
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public List<TData> Datas { get; set; }
    }
}
