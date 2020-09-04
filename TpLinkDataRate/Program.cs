using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using TpLink.Api;

namespace TpLinkDataRate
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
            .ConfigureServices(/*async*/(hostContext, services) =>
            {
                string login = Environment.GetEnvironmentVariable("tplink_powerline_login", EnvironmentVariableTarget.User);
                string password = Environment.GetEnvironmentVariable("tplink_powerline_pwd", EnvironmentVariableTarget.User);

                // note: ensure the vpn is turned off / net
                // can also be checked here: Control Panel\Network and Internet\Network Connections

                // find ip of the powerline
                string ip = TpLinkClient.DiscoveryAsync().GetAwaiter().GetResult(); // NOTE: using async here may break the DI pattern and throw CreateHostBuilder(args).Build().Run(); 

                Console.WriteLine($"found ip: {ip}");

                services.AddHostedService<Worker>();
                //services.AddSingleton<IRestClient, RestClient>();
                services.AddSingleton<ITpLinkClient>(new TpLinkClient(login, password, $"http://{ip}/"));
            });
        }
    }
}
