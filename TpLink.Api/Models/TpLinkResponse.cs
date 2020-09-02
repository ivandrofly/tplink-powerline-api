using System.Text.Json.Serialization;
using TpLink.Api.Models;

namespace TpLink.Models
{
    public class TpLinkResponse<TData> : IResponseModel<TData>
    {
        public bool Timeout { get; set; }
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public TData Data { get; set; }
    }
}
