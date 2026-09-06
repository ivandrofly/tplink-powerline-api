using RestSharp;
using TpLink.Api;
using TpLink.Api.Models;
using Xunit;

namespace TpLink.UnitTest;

public class FormFieldsTest
{
    [Fact]
    public void EmitsLowerCasedAndAttributedNamesAndSkipsNulls()
    {
        var model = new WirelessModel
        {
            Enable = "on",
            SSID = "MyWifi",
            PskKey = "secret",
            Hwmode = "a",
            // everything else stays null and must not be emitted as an empty field
        };
        var req = new RestRequest("admin/wireless", Method.Post);

        TpLinkClient.AddFormFields(req, model);

        var fields = req.Parameters
            .Where(p => p.Type == ParameterType.GetOrPost)
            .ToDictionary(p => p.Name!, p => (string?)p.Value);

        Assert.Equal(4, fields.Count);
        Assert.Equal("on", fields["enable"]);
        Assert.Equal("MyWifi", fields["ssid"]);
        Assert.Equal("secret", fields["psk_key"]);
        Assert.Equal("a", fields["hwmode"]);
        Assert.DoesNotContain("wireless_2g_disabled", fields.Keys);
    }

    [Fact]
    public void AppliesConvertersToNonStringProperties()
    {
        var schedule = new WifiSchedule
        {
            Enable = true,
            StartTime = 20,
            EndTime = 23,
            Days = Days.Monday | Days.Sunday,
        };
        var req = new RestRequest("admin/wlanTimeControl", Method.Post);

        TpLinkClient.AddFormFields(req, schedule);

        var fields = req.Parameters.ToDictionary(p => p.Name!, p => (string?)p.Value);
        Assert.Equal("on", fields["enable"]);
        Assert.Equal("20", fields["stime"]);
        Assert.Equal("23", fields["etime"]);
        Assert.Equal("3", fields["days"]);
        Assert.Equal("1", fields["week_mon"]);
        Assert.Equal("0", fields["week_tues"]);
        Assert.Equal("1", fields["week_sun"]);
    }
}
