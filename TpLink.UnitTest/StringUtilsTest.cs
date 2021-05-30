using System;
using System.Text.Json;
using System.Threading.Tasks;
using TpLink.Api;
using TpLink.Api.Helpers;
using TpLink.Api.Models;
using Xunit;
using Xunit.Sdk;

namespace TpLink.UnitTest
{
    public class StringUtilsTest
    {
        [Theory]
        [InlineData("ethereum", "bitcoin")]
        [InlineData("ethereum", "bitcoin ", Skip = "Miss match")]
        public void TestHashHashMethod(string userName, string password)
        {
            Assert.Equal("Basic%20ethereum%3Acd5b1e4947e304476c788cd474fb579a",
                StringUtils.GetAuthorization(userName, password));
        }

        [Fact]
        public void WifiScheduleTest()
        {
            var wifiSchedule = new WifiSchedule
            {
                Enable = true,
                StartTime = 0,
                EndTime = 1,
                Days = Days.Monday | Days.Tuesday | Days.Wednesday | Days.Thursday | Days.Friday | Days.Saturday | Days.Sunday
            };
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                IgnoreNullValues = true,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
            };
            var output = JsonSerializer.Serialize(wifiSchedule, jsonOptions);

            const string Expected =
                "{\"stime\":\"0\",\"etime\":\"1\",\"days\":\"127\",\"enable\":\"on\",\"week_mon\":\"1\",\"week_tues\":\"1\",\"week_wed\":\"1\",\"week_thur\":\"1\",\"week_fri\":\"1\",\"week_sat\":\"1\",\"week_sun\":\"1\"}";
            Assert.Equal(Expected, output);
            
            
            // test 2
            var ws = new WifiSchedule
            {
                Enable = true,
                StartTime = 0,
                EndTime = 1,
                Days = Days.Monday | Days.Tuesday
            };
            const string Expected2 =
                "{\"stime\":\"0\",\"etime\":\"1\",\"days\":\"6\",\"enable\":\"on\",\"week_mon\":\"1\",\"week_tues\":\"1\",\"week_wed\":\"0\",\"week_thur\":\"0\",\"week_fri\":\"0\",\"week_sat\":\"0\",\"week_sun\":\"0\"}";
            var output2 = JsonSerializer.Serialize(ws, jsonOptions);
            Assert.Equal(Expected2, output2);
            
            // note the days: will change also:
            // 0001 : monday
            // 0011 : monday, tuesday
            // ...
        }
        
        // todo: 
        // - test login
        // - check if not on vpn
        // - test reboot (after reboot waiting 3 sec at least and recheck for connection to router, (maybe ping)
    }
}