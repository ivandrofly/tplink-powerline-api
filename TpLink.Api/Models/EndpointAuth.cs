using TpLink.Api.Helpers;

namespace TpLink.Api.Models;

/// <summary>
/// Where the adapter is and how to log in. Immutable, and the password is never exposed: only the
/// authorization cookie derived from it is handed to the client.
/// </summary>
public sealed class EndpointAuth
{
    private readonly string _password;

    public EndpointAuth(string login, string password, string endpoint)
    {
        Login = Require(login, nameof(login));
        _password = Require(password, nameof(password));
        Endpoint = Require(endpoint, nameof(endpoint));

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException($"Endpoint must be an absolute http(s) URL such as http://192.168.1.1, got '{endpoint}'.", nameof(endpoint));
        }
    }

    /// <summary>Web-admin login, usually <c>admin</c>.</summary>
    public string Login { get; }

    /// <summary>Adapter base URL, e.g. <c>http://192.168.1.86</c>.</summary>
    public string Endpoint { get; }

    /// <summary>
    /// The value of the <c>Authorization</c> cookie the adapter expects: <c>Basic {login}:{md5(password)}</c>, URL-escaped.
    /// </summary>
    internal string BuildAuthorizationCookie() => StringUtils.GetAuthorization(Login, _password);

    public override string ToString() => $"{Login}@{Endpoint}";

    private static string Require(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required but was null or white space. Check the credentials read from configuration or the environment.", name);
        }

        return value;
    }
}
