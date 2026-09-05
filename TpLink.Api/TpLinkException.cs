using System.Net;

namespace TpLink.Api;

/// <summary>
/// Thrown when a request to the adapter does not produce a JSON answer: the connection failed or timed out,
/// or the device replied with a non-success HTTP status or an empty body.
/// </summary>
/// <remarks>
/// A request the device accepted but rejected (for example while its web manager is open in a browser) does
/// not throw; it returns a <see cref="Models.TpLinkResponse{TData}"/> whose <c>Success</c> is <c>false</c>.
/// A body that is not the expected JSON (encrypted firmware) surfaces as <see cref="System.Text.Json.JsonException"/>.
/// </remarks>
public class TpLinkException : Exception
{
    public TpLinkException(string message) : base(message)
    {
    }

    public TpLinkException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>HTTP status of the response, or 0 when no response was received.</summary>
    public HttpStatusCode StatusCode { get; init; }

    /// <summary>
    /// True when the request timed out. RestSharp reports a timeout as <c>ResponseStatus.TimedOut</c> with
    /// status code 0, never as HTTP 408.
    /// </summary>
    public bool TimedOut { get; init; }
}
