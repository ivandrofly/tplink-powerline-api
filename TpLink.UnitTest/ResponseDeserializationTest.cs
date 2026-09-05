using System.Collections.Generic;
using System.Text.Json;
using TpLink.Api;
using TpLink.Api.Models;
using Xunit;

namespace TpLink.UnitTest
{
    /// <summary>
    /// Pins the envelope mapping with the client's shared options. AddNewWifiScheduleAsync used to deserialize
    /// without them, so "success" never reached Success and every caller saw false.
    /// </summary>
    public class ResponseDeserializationTest
    {
        [Fact]
        public void EnvelopeFlagsMapCaseInsensitively()
        {
            const string json = "{\"success\":true,\"timeout\":false,\"data\":{\"enable\":\"on\",\"ssid\":\"MyWifi\",\"psk_key\":\"secret\"}}";

            var response = JsonSerializer.Deserialize<TpLinkResponse<WirelessModel>>(json, TpLinkClient.JsonOptions);

            Assert.True(response.Success);
            Assert.False(response.Timeout);
            Assert.Equal("on", response.Data.Enable);
            Assert.Equal("MyWifi", response.Data.SSID);
            Assert.Equal("secret", response.Data.PskKey);
        }

        [Fact]
        public void RejectedRequestLeavesDataNull()
        {
            const string json = "{\"success\":false,\"timeout\":true}";

            var response = JsonSerializer.Deserialize<TpLinkResponse<WirelessModel>>(json, TpLinkClient.JsonOptions);

            Assert.False(response.Success);
            Assert.True(response.Timeout);
            Assert.Null(response.Data);
        }

        [Fact]
        public void ScheduleListRoundTrips()
        {
            const string json = "{\"success\":true,\"timeout\":false,\"data\":[{\"stime\":\"20\",\"etime\":\"23\",\"days\":\"127\",\"week_sun\":\"1\",\"week_mon\":\"1\",\"week_tues\":\"1\",\"week_wed\":\"1\",\"week_thur\":\"1\",\"week_fri\":\"1\",\"week_sat\":\"1\",\"enable\":\"on\"}]}";

            var response = JsonSerializer.Deserialize<TpLinkResponse<ICollection<WifiSchedule>>>(json, TpLinkClient.JsonOptions);

            Assert.True(response.Success);
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

            Assert.Equal("64", response.MaxRules);
            var client = Assert.Single(response.Data);
            Assert.Equal("phone", client.DeviceName);
            Assert.Equal("192.168.1.50", client.IP);
            Assert.Equal("10", client.ReceivedPackets);
        }
    }
}
