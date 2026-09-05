using TpLink.Api;
using TpLink.Api.Models;
using Xunit;

namespace TpLink.UnitTest;

public class WifiScheduleValidationTest
{
    // validation runs before any request, so an unreachable endpoint is fine here
    private static TpLinkClient NewClient() => new("admin", "pwd", "http://192.0.2.1");

    [Fact]
    public async Task RejectsNull()
    {
        using var client = NewClient();
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.AddNewWifiScheduleAsync(null!));
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(0, 25)]
    [InlineData(25, 26)]
    public async Task RejectsHoursOutsideTheDay(int start, int end)
    {
        using var client = NewClient();
        var schedule = new WifiSchedule { StartTime = start, EndTime = end, Days = Days.Monday };
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.AddNewWifiScheduleAsync(schedule));
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(6, 5)]
    public async Task RejectsStartNotBeforeEnd(int start, int end)
    {
        using var client = NewClient();
        var schedule = new WifiSchedule { StartTime = start, EndTime = end, Days = Days.Monday };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.AddNewWifiScheduleAsync(schedule));
        Assert.Equal("wifiSchedule", ex.ParamName);
    }

    [Fact]
    public async Task DisposedClientThrows()
    {
        var client = NewClient();
        client.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.GetClientsAsync());
    }
}
