using System;

namespace TpLink.Api
{
    /// <summary>
    /// Settings for <see cref="TpLinkClient.CreateAsync"/>. Bind it from a configuration section named
    /// <see cref="SectionName"/> (appsettings.json, user-secrets or <c>TpLink__Login</c>-style environment variables),
    /// then call <see cref="ApplyEnvironmentFallback"/> to honour the original <c>tplink_powerline_*</c> variables.
    /// </summary>
    public class TpLinkOptions
    {
        public const string SectionName = "TpLink";

        /// <summary>Environment variable the sample apps have always read the login from.</summary>
        public const string LoginVariable = "tplink_powerline_login";

        /// <summary>Environment variable the sample apps have always read the password from.</summary>
        public const string PasswordVariable = "tplink_powerline_pwd";

        /// <summary>Optional environment variable with the adapter URL; skips discovery when set.</summary>
        public const string EndpointVariable = "tplink_powerline_endpoint";

        /// <summary>Web-admin login, usually <c>admin</c>.</summary>
        public string Login { get; set; }

        /// <summary>Web-admin password.</summary>
        public string Password { get; set; }

        /// <summary>Adapter URL such as <c>http://192.168.1.86</c>. Leave empty to discover the adapter with a UDP broadcast.</summary>
        public string Endpoint { get; set; }

        /// <summary>Per-request HTTP timeout.</summary>
        public TimeSpan RequestTimeout { get; set; } = TpLinkClient.DefaultRequestTimeout;

        /// <summary>How long discovery waits for an adapter to answer.</summary>
        public TimeSpan DiscoveryTimeout { get; set; } = TpLinkClient.DefaultDiscoveryTimeout;

        /// <summary>
        /// Fill any empty value from the <c>tplink_powerline_login</c>, <c>tplink_powerline_pwd</c> and
        /// <c>tplink_powerline_endpoint</c> process environment variables.
        /// </summary>
        public TpLinkOptions ApplyEnvironmentFallback()
        {
            Login = FirstNonEmpty(Login, Environment.GetEnvironmentVariable(LoginVariable));
            Password = FirstNonEmpty(Password, Environment.GetEnvironmentVariable(PasswordVariable));
            Endpoint = FirstNonEmpty(Endpoint, Environment.GetEnvironmentVariable(EndpointVariable));
            return this;
        }

        /// <exception cref="InvalidOperationException">Login or password is missing, or a timeout is not positive.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Login))
            {
                throw Missing(nameof(Login), LoginVariable);
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                throw Missing(nameof(Password), PasswordVariable);
            }

            if (RequestTimeout <= TimeSpan.Zero)
            {
                throw new InvalidOperationException($"{SectionName}:{nameof(RequestTimeout)} must be positive.");
            }

            if (DiscoveryTimeout <= TimeSpan.Zero)
            {
                throw new InvalidOperationException($"{SectionName}:{nameof(DiscoveryTimeout)} must be positive.");
            }
        }

        private static InvalidOperationException Missing(string property, string variable) =>
            new InvalidOperationException(
                $"The TP-Link {property.ToLowerInvariant()} is not configured. Set '{SectionName}:{property}' in configuration " +
                $"(appsettings.json, user-secrets or the {SectionName}__{property} environment variable) " +
                $"or the {variable} environment variable (setx on Windows, export on Linux/macOS, then open a new terminal).");

        private static string FirstNonEmpty(string preferred, string fallback) =>
            string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
    }
}
