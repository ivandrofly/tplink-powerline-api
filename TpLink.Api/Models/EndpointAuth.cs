using System;

namespace TpLink.Api.Models
{
    public class EndpointAuth
    {
        public EndpointAuth(string login, string passoword, string endpoint)
        {
            Login = Require(login, nameof(login));
            Passoword = Require(passoword, nameof(passoword));
            Endpoint = Require(endpoint, nameof(endpoint));

            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException($"Endpoint must be an absolute http(s) URL such as http://192.168.1.1, got '{endpoint}'.", nameof(endpoint));
            }
        }

        public string Login { get; set; }
        public string Passoword { get; set; }
        public string Endpoint { get; set; }

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // a null here used to surface much later as "ArgumentNullException: s" from the MD5 hashing
                throw new ArgumentException($"{name} is required but was null or white space. Check the credentials read from configuration or the environment.", name);
            }

            return value;
        }
    }
}
