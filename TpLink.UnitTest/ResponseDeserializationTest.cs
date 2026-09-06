using System.Text.Json;
using TpLink.Api;
using TpLink.Api.Models;
using Xunit;

namespace TpLink.UnitTest;

/// <summary>
/// Pins the envelope mapping with the client's shared options against payloads shaped like the device's.
/// </summary>
public class ResponseDeserializationTest
{
    [Fact]
    public void EnvelopeFlagsMapCaseInsensitively()
    {
        const string json = "{\"success\":true,\"timeout\":false,\"data\":{\"enable\":\"on\",\"ssid\":\"MyWifi\",\"psk_key\":\"secret\"}}";

        var response = JsonSerializer.Deserialize<TpLinkResponse<WirelessModel>>(json, TpLinkClient.JsonOptions);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.False(response.Timeout);
        Assert.NotNull(response.Data);
        Assert.Equal("on", response.Data.Enable);
        Assert.Equal("MyWifi", response.Data.SSID);
        Assert.Equal("secret", response.Data.PskKey);
    }

    [Fact]
    public void RejectedRequestLeavesDataNull()
    {
        const string json = "{\"success\":false,\"timeout\":true}";

        var response = JsonSerializer.Deserialize<TpLinkResponse<WirelessModel>>(json, TpLinkClient.JsonOptions);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.True(response.Timeout);
        Assert.Null(response.Data);
    }

    [Fact]
    public void ScheduleListRoundTrips()
    {
        const string json = "{\"success\":true,\"timeout\":false,\"data\":[{\"stime\":\"20\",\"etime\":\"23\",\"days\":\"127\",\"week_sun\":\"1\",\"week_mon\":\"1\",\"week_tues\":\"1\",\"week_wed\":\"1\",\"week_thur\":\"1\",\"week_fri\":\"1\",\"week_sat\":\"1\",\"enable\":\"on\"}]}";

        var response = JsonSerializer.Deserialize<TpLinkResponse<ICollection<WifiSchedule>>>(json, TpLinkClient.JsonOptions);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        var rule = Assert.Single(response.Data);
        Assert.Equal(20, rule.StartTime);
        Assert.Equal(23, rule.EndTime);
        Assert.Equal((Days)127, rule.Days);
        Assert.True(rule.Enable);
        Assert.True(rule.Sunday);
    }

    [Fact]
    public void ClientListCarriesMaxRules()
    {
        const string json = "{\"success\":true,\"timeout\":false,\"max_rules\":\"64\",\"data\":[{\"mac\":\"AA-BB-CC-DD-EE-FF\",\"type\":\"2.4GHz\",\"encryption\":\"wpa\",\"rxpkts\":\"10\",\"txpkts\":\"20\",\"ip\":\"192.168.1.50\",\"devName\":\"phone\"}]}";

        var response = JsonSerializer.Deserialize<TpLinkClientData>(json, TpLinkClient.JsonOptions);

        Assert.NotNull(response);
        Assert.Equal("64", response.MaxRules);
        Assert.NotNull(response.Data);
        var client = Assert.Single(response.Data);
        Assert.Equal("phone", client.DeviceName);
        Assert.Equal("192.168.1.50", client.IP);
        Assert.Equal("10", client.ReceivedPackets);
    }

    [Theory]
    [InlineData("2g")]
    [InlineData("5g")]
    public void GuestNetworkReadsThePrefixedFieldsOfEitherBand(string band)
    {
        var json = $"{{\"success\":true,\"timeout\":false,\"data\":{{\"guest_{band}_enable\":\"on\",\"guest_{band}_disabled\":\"off\",\"guest_{band}_hidden\":\"off\",\"guest_{band}_ssid\":\"Guests\",\"guest_{band}_psk_key\":\"letmein\",\"guest_{band}_encryption\":\"psk\"}}}}";
        var options = band == "2g" ? TpLinkClient.Guest2GJsonOptions : TpLinkClient.Guest5GJsonOptions;

        var response = JsonSerializer.Deserialize<TpLinkResponse<GuestNetwork>>(json, options);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("on", response.Data.Enable);
        Assert.Equal("off", response.Data.Disabled);
        Assert.Equal("Guests", response.Data.Ssid);
        Assert.Equal("letmein", response.Data.PskKey);
        Assert.Equal("psk", response.Data.Encryption);
    }

    [Fact]
    public void GuestOptionsDoNotMatchTheOtherBand()
    {
        const string json = "{\"success\":true,\"data\":{\"guest_5g_ssid\":\"Guests\"}}";

        var response = JsonSerializer.Deserialize<TpLinkResponse<GuestNetwork>>(json, TpLinkClient.Guest2GJsonOptions);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Null(response.Data.Ssid);
    }
}
