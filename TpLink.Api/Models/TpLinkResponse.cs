using System.Text.Json.Serialization;

namespace TpLink.Api.Models
{
    public class TpLinkResponse<TData> : IResponseModel<TData>
    {
        public bool Timeout { get; set; }
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public TData Data { get; set; }
    }
}
