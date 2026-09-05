using System;
using TpLink.Api;
using Xunit;

namespace TpLink.UnitTest
{
    public class TpLinkOptionsTest
    {
        [Fact]
        public void ValidateNamesTheMissingLogin()
        {
            var options = new TpLinkOptions { Password = "pwd" };
            var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("TpLink:Login", ex.Message);
            Assert.Contains(TpLinkOptions.LoginVariable, ex.Message);
        }

        [Fact]
        public void ValidateNamesTheMissingPassword()
        {
            var options = new TpLinkOptions { Login = "admin" };
            var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("TpLink:Password", ex.Message);
            Assert.Contains(TpLinkOptions.PasswordVariable, ex.Message);
        }

        [Fact]
        public void ValidateRejectsNonPositiveTimeouts()
        {
            var options = new TpLinkOptions { Login = "admin", Password = "pwd", RequestTimeout = TimeSpan.Zero };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void EnvironmentFallbackOnlyFillsEmptyValues()
        {
            Environment.SetEnvironmentVariable(TpLinkOptions.LoginVariable, "env-login");
            Environment.SetEnvironmentVariable(TpLinkOptions.PasswordVariable, "env-pwd");
            Environment.SetEnvironmentVariable(TpLinkOptions.EndpointVariable, "http://10.0.0.5");
            try
            {
                var options = new TpLinkOptions { Login = "configured" }.ApplyEnvironmentFallback();

                Assert.Equal("configured", options.Login);
                Assert.Equal("env-pwd", options.Password);
                Assert.Equal("http://10.0.0.5", options.Endpoint);
            }
            finally
            {
                Environment.SetEnvironmentVariable(TpLinkOptions.LoginVariable, null);
                Environment.SetEnvironmentVariable(TpLinkOptions.PasswordVariable, null);
                Environment.SetEnvironmentVariable(TpLinkOptions.EndpointVariable, null);
            }
        }

        [Fact]
        public void DefaultsMatchTheClient()
        {
            var options = new TpLinkOptions();
            Assert.Equal(TpLinkClient.DefaultRequestTimeout, options.RequestTimeout);
            Assert.Equal(TpLinkClient.DefaultDiscoveryTimeout, options.DiscoveryTimeout);
        }
    }
}
