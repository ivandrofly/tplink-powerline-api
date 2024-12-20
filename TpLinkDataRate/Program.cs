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

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
            .ConfigureServices(/*async*/ (hostContext, services) =>
            {
                string login = Environment.GetEnvironmentVariable("tplink_powerline_login", EnvironmentVariableTarget.User);
                string password = Environment.GetEnvironmentVariable("tplink_powerline_pwd", EnvironmentVariableTarget.User);

                // note: ensure the vpn is turned off / net
                // can also be checked here: Control Panel\Network and Internet\Network Connections

                // find ip of the powerline | if you have vpn or several adapter, make sure this is sending dicovery packet to
                // the network where powerline is connected to
                string ip = TpLinkClient.DiscoveryAsync().GetAwaiter().GetResult(); // NOTE: using async here may break the DI pattern and throw CreateHostBuilder(args).Build().Run(); 

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
