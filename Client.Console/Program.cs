using System;
using TpLink.Api;
using TpLink.Api.Models;

Console.WriteLine("sending the discovery request...");

bool discover = true;

string endpoint = discover ? await TpLinkClient.DiscoveryAsync() : "192.168.1.97";

Console.WriteLine($"tp link address: {endpoint}");

// wait for 5 secs befor resuming

// get credentials from system environmetn variables
string login = Environment.GetEnvironmentVariable("tplink_powerline_login", EnvironmentVariableTarget.User);
string password = Environment.GetEnvironmentVariable("tplink_powerline_pwd", EnvironmentVariableTarget.User);

#pragma warning disable CS0612 // Type or member is obsolete
var client = new TpLink.Api.TpLinkClient(login, password, $"http://{endpoint}");
#pragma warning restore CS0612 // Type or member is obsolete


//todo: bug here!!!
var res = await client.GetSystemLogsAsync();
foreach (TpLink.Models.SystemLog item in res.Data)
{
    Console.WriteLine(item.Content);
}

// to
var wifiSchedule = new WifiSchedule
{
    Enable = true,
    StartTime = 0,
    EndTime = 1,
    Days = Days.Monday | Days.Tuesday | Days.Wednesday | Days.Thursday | Days.Friday | Days.Saturday | Days.Sunday
};

var response = await client.AddNewWifiScheduleAsync(wifiSchedule).ConfigureAwait(false);
Console.WriteLine(response);