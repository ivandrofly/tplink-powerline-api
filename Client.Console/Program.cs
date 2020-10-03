using System;
using System.Threading.Tasks;
using TpLink.Api;


Console.WriteLine("sending the discovery request...");
string endpoint = await TpLinkClient.DiscoveryAsync();

Console.WriteLine($"tp link address: {endpoint}");

// wait for 5 secs befor resuming

// get credentials from system environmetn variables
string login = Environment.GetEnvironmentVariable("tplink_powerline_login", EnvironmentVariableTarget.User);
string password = Environment.GetEnvironmentVariable("tplink_powerline_pwd", EnvironmentVariableTarget.User);

#pragma warning disable CS0612 // Type or member is obsolete
var client = new TpLink.Api.TpLinkClient(login, password, $"http://{endpoint}");
#pragma warning restore CS0612 // Type or member is obsolete


//Console.WriteLine("rebooting...");
//await client.RebootAsync();
//Console.WriteLine("reboot done!");
Console.WriteLine();
foreach (var clientWireless in (await client.GetClientsAsync()).Data)
{
    Console.WriteLine(clientWireless.DeviceName);
}
Console.WriteLine();

// todo:  bug here!!!
//var res = await client.GetSystemLogsAsync();
//foreach (var item in res.Data)
//{
//    Console.WriteLine(item.Content);
//}


Task.Delay(1000 * 10).GetAwaiter().GetResult();