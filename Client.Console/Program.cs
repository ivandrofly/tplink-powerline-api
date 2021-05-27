using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Client.Console;
using Client.Console.Commands;
using TpLink.Api;
using TpLink.Api.Models;


var invoker = new Invoker();

// https://stackoverflow.com/questions/23048285/call-asynchronous-method-in-constructor
await invoker.DiscoverAsync();
//await invoker.DisplayClient();
// await invoker.RunBatch();

await invoker.TurnOn();
//await invoker.Reboot();


// note: make sure you request is not being tunneled by vpn
//var result = NetworkInterface.GetAllNetworkInterfaces().First(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet);
//var ips = result.GetIPProperties();
//var v4IPv4Statistics = result.GetIPv4Statistics();

// to
var wifiSchedule = new WifiSchedule
{
    Enable = true,
    StartTime = 0,
    EndTime = 1,
    Days = Days.Monday | Days.Tuesday | Days.Wednesday | Days.Thursday | Days.Friday | Days.Saturday | Days.Sunday
};

//var response = await client.AddNewWifiScheduleAsync(wifiSchedule).ConfigureAwait(false);
//Console.WriteLine(response);
Console.ReadLine();