using TpLink.Api;
using Xunit;

namespace TpLink.UnitTest;

public class CreateAsyncTest
{
    private sealed class FakeDiscovery(string? ip) : ITpLinkDiscovery
    {
        public int Calls { get; private set; }

        public Task<string> DiscoverAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            Calls++;
            return ip is null
                ? throw new TimeoutException("no adapter")
                : Task.FromResult(ip);
        }
    }

    [Fact]
    public async Task DiscoversWhenNoEndpointIsConfigured()
    {
        var discovery = new FakeDiscovery("10.0.0.7");
        var options = new TpLinkOptions { Login = "admin", Password = "pwd" };

        using var client = await TpLinkClient.CreateAsync(options, discovery);

        Assert.Equal("http://10.0.0.7", client.Endpoint);
        Assert.Equal(1, discovery.Calls);
    }

    [Fact]
    public async Task SkipsDiscoveryWhenAnEndpointIsConfigured()
    {
        var discovery = new FakeDiscovery(null);
        var options = new TpLinkOptions { Login = "admin", Password = "pwd", Endpoint = "http://192.168.1.86" };

        using var client = await TpLinkClient.CreateAsync(options, discovery);

        Assert.Equal("http://192.168.1.86", client.Endpoint);
        Assert.Equal(0, discovery.Calls);
    }

    [Fact]
    public async Task PropagatesDiscoveryTimeout()
    {
        var options = new TpLinkOptions { Login = "admin", Password = "pwd" };

        await Assert.ThrowsAsync<TimeoutException>(() => TpLinkClient.CreateAsync(options, new FakeDiscovery(null)));
    }

    [Fact]
    public async Task ValidatesBeforeDiscovering()
    {
        var discovery = new FakeDiscovery("10.0.0.7");
        var options = new TpLinkOptions { Login = "admin" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => TpLinkClient.CreateAsync(options, discovery));
        Assert.Equal(0, discovery.Calls);
    }
}
