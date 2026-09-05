using TpLink.Api.Models;
using Xunit;

namespace TpLink.UnitTest;

public class EndpointAuthTest
{
    [Theory]
    [InlineData(null, "pwd", "http://192.168.1.1", "login")]
    [InlineData("admin", null, "http://192.168.1.1", "password")]
    [InlineData("admin", "  ", "http://192.168.1.1", "password")]
    [InlineData("admin", "pwd", null, "endpoint")]
    [InlineData("admin", "pwd", "192.168.1.1", "endpoint")]
    [InlineData("admin", "pwd", "ftp://192.168.1.1", "endpoint")]
    public void RejectsMissingOrInvalidValues(string? login, string? password, string? endpoint, string expectedParam)
    {
        var ex = Assert.Throws<ArgumentException>(() => new EndpointAuth(login!, password!, endpoint!));
        Assert.Equal(expectedParam, ex.ParamName);
    }

    [Theory]
    [InlineData("http://192.168.1.1")]
    [InlineData("http://192.168.1.86/")]
    [InlineData("https://powerline.local")]
    public void AcceptsAbsoluteHttpEndpoints(string endpoint)
    {
        var auth = new EndpointAuth("admin", "pwd", endpoint);
        Assert.Equal(endpoint, auth.Endpoint);
    }

    [Fact]
    public void ToStringDoesNotLeakThePassword()
    {
        var auth = new EndpointAuth("admin", "hunter2", "http://192.168.1.1");
        Assert.DoesNotContain("hunter2", auth.ToString());
    }
}
