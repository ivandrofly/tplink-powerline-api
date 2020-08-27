namespace TpLink.Api.Models
{
    public interface IResponseModel<TData>
    {
        bool Timeout { get; set; }
        bool Success { get; set; }
        TData Data { get; set; }
    }
}
