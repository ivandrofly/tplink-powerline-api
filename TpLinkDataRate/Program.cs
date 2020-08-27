using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TpLink.Api;

namespace TpLinkDataRate
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    //services.AddSingleton<IRestClient, RestClient>();
                    services.AddSingleton<ITpLinkClient>(new TpLinkClient("admin", "2207psp", "http://192.168.1.86/"));
                });
    }
}
