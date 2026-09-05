using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TpLink.Api;

namespace TpLink.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // "TpLink" section from appsettings.json, user-secrets (Development) or TpLink__* environment
                    // variables; the original tplink_powerline_* variables still work as a fallback. Discovery, when
                    // no endpoint is configured, happens inside the worker so the host never blocks on it.
                    services.Configure<TpLinkOptions>(context.Configuration.GetSection(TpLinkOptions.SectionName));
                    services.PostConfigure<TpLinkOptions>(options => options.ApplyEnvironmentFallback());
                    services.Configure<WorkerOptions>(context.Configuration.GetSection(WorkerOptions.SectionName));
                    services.AddHostedService<Worker>();
                })
                .Build()
                .Run();
        }
    }
}
