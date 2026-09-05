using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TpLink.Api;

namespace TpLink.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();

            Console.WriteLine("done");
            Console.ReadLine();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices( /*async*/ (hostContext, services) =>
                {
                    // read the process environment: user-scoped variables (setx) are inherited on Windows, and this is
                    // the only scope that exists on Linux/macOS, where EnvironmentVariableTarget.User always returns null
                    string login = Environment.GetEnvironmentVariable("tplink_powerline_login");
                    string password = Environment.GetEnvironmentVariable("tplink_powerline_pwd");
                    if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                    {
                        throw new InvalidOperationException(
                            "Set the tplink_powerline_login and tplink_powerline_pwd environment variables " +
                            "(setx on Windows, export on Linux/macOS) and open a new terminal.");
                    }

                    // note: ensure the vpn is turned off / net
                    // can also be checked here: Control Panel\Network and Internet\Network Connections

                    // find ip of the powerline | if you have vpn or several adapter, make sure this is sending dicovery packet to
                    // the network where powerline is connected to
                    var ip = TpLinkClient.DiscoveryAsync().GetAwaiter()
                            .GetResult(); // NOTE: using async here may break the DI pattern and throw CreateHostBuilder(args).Build().Run(); 

                    // won't work (thread problem)
                    //string ip = await TpLinkClient.DiscoveryAsync();

                    Console.WriteLine($"found ip: {ip}");

                    services.AddHostedService<Worker>();
                    //services.AddSingleton<IRestClient, RestClient>();
                    services.AddSingleton<ITpLinkClient>(new TpLinkClient(login, password, $"http://{ip}/"));
                });
        }
    }
}