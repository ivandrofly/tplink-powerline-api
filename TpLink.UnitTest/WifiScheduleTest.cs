using System.Text.Json;
using System.Text.Json.Serialization;
using TpLink.Api;
using TpLink.Api.Models;
using Xunit;

namespace TpLink.UnitTest
{
    /// <summary>
    /// Pins the payload the adapter expects for a schedule rule:
    /// {"stime":"20","etime":"23","days":"127","week_sun":"1",...,"enable":"on"}
    /// </summary>
    public class WifiScheduleTest
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
        };

        private const string EveryDay =
            "{\"stime\":\"0\",\"etime\":\"1\",\"days\":\"127\",\"enable\":\"on\",\"week_mon\":\"1\",\"week_tues\":\"1\",\"week_wed\":\"1\",\"week_thur\":\"1\",\"week_fri\":\"1\",\"week_sat\":\"1\",\"week_sun\":\"1\"}";

        [Fact]
        public void SerializesEveryDay()
        {
            var schedule = new WifiSchedule
            {
                Enable = true,
                StartTime = 0,
                EndTime = 1,
                Days = Days.Monday | Days.Tuesday | Days.Wednesday | Days.Thursday | Days.Friday | Days.Saturday | Days.Sunday,
            };

            Assert.Equal(EveryDay, JsonSerializer.Serialize(schedule, Options));
        }

        [Fact]
        public void SerializesTwoDays()
        {
            var schedule = new WifiSchedule { Enable = true, StartTime = 0, EndTime = 1, Days = Days.Monday | Days.Tuesday };

            const string expected =
                "{\"stime\":\"0\",\"etime\":\"1\",\"days\":\"6\",\"enable\":\"on\",\"week_mon\":\"1\",\"week_tues\":\"1\",\"week_wed\":\"0\",\"week_thur\":\"0\",\"week_fri\":\"0\",\"week_sat\":\"0\",\"week_sun\":\"0\"}";
            Assert.Equal(expected, JsonSerializer.Serialize(schedule, Options));
        }

        [Theory]
        [InlineData(Days.Sunday, "week_sun", "1")]
        [InlineData(Days.Monday, "week_mon", "2")]
        [InlineData(Days.Tuesday, "week_tues", "4")]
        [InlineData(Days.Wednesday, "week_wed", "8")]
        [InlineData(Days.Thursday, "week_thur", "16")]
        [InlineData(Days.Friday, "week_fri", "32")]
        [InlineData(Days.Saturday, "week_sat", "64")]
        public void EachDaySetsExactlyItsOwnBit(Days day, string field, string bitmask)
        {
            var schedule = new WifiSchedule { Enable = false, StartTime = 8, EndTime = 9, Days = day };

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(schedule, Options));
            var root = json.RootElement;

            Assert.Equal(bitmask, root.GetProperty("days").GetString());
            Assert.Equal("off", root.GetProperty("enable").GetString());
            foreach (var name in new[] { "week_sun", "week_mon", "week_tues", "week_wed", "week_thur", "week_fri", "week_sat" })
            {
                Assert.Equal(name == field ? "1" : "0", root.GetProperty(name).GetString());
            }
        }

        [Fact]
        public void NoDaysClearsEveryBit()
        {
            var schedule = new WifiSchedule { Enable = true, StartTime = 8, EndTime = 9, Days = 0 };

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(schedule, Options));
            Assert.Equal("0", json.RootElement.GetProperty("days").GetString());
            Assert.Equal("0", json.RootElement.GetProperty("week_mon").GetString());
            Assert.Equal("0", json.RootElement.GetProperty("week_sun").GetString());
        }

        [Fact]
        public void SharedClientOptionsProduceTheSamePayload()
        {
            // every property carries JsonPropertyName, so the client's lower-casing policy must not alter the payload
            var schedule = new WifiSchedule
            {
                Enable = true,
                StartTime = 0,
                EndTime = 1,
                Days = Days.Monday | Days.Tuesday | Days.Wednesday | Days.Thursday | Days.Friday | Days.Saturday | Days.Sunday,
            };

            Assert.Equal(EveryDay, JsonSerializer.Serialize(schedule, TpLinkClient.JsonOptions));
        }

        [Fact]
        public void DeserializesTheDevicePayloadAndDaysWins()
        {
            // the week_* properties are derived from Days and have no setter, so a contradictory week_* value is ignored
            const string json = "{\"stime\":\"20\",\"etime\":\"23\",\"days\":\"3\",\"week_sun\":\"1\",\"week_mon\":\"1\",\"week_tues\":\"1\",\"enable\":\"on\"}";

            var schedule = JsonSerializer.Deserialize<WifiSchedule>(json, TpLinkClient.JsonOptions);

            Assert.Equal(20, schedule.StartTime);
            Assert.Equal(23, schedule.EndTime);
            Assert.Equal(Days.Sunday | Days.Monday, schedule.Days);
            Assert.True(schedule.Enable);
            Assert.True(schedule.Sunday);
            Assert.True(schedule.Monday);
            Assert.False(schedule.Tuesday);
        }
    }
}
