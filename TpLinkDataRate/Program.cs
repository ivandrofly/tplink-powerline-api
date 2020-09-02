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
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                string login = Environment.GetEnvironmentVariable("tplink_powerline_login", EnvironmentVariableTarget.User);
                string password = Environment.GetEnvironmentVariable("tplink_powerline_pwd", EnvironmentVariableTarget.User);

                services.AddHostedService<Worker>();
                //services.AddSingleton<IRestClient, RestClient>();
                services.AddSingleton<ITpLinkClient>(new TpLinkClient(login, password, "http://192.168.1.86/"));
            });
        }
    }
}
