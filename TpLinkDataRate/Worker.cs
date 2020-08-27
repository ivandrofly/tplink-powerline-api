using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Extensions;
using RestSharp.Serialization.Json;
using RestSharp.Serializers.SystemTextJson;
using TpLink.Api;

namespace TpLinkDataRate
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ITpLinkClient _tpLinkClient;

        public Worker(ILogger<Worker> logger, ITpLinkClient tpLinkClient)
        {
            _logger = logger;
            _tpLinkClient = tpLinkClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            while (!stoppingToken.IsCancellationRequested)
            {
                // the default jsonserializer (newtownsoft) is faillign to handle tplink direclty
                // var response = await restClient.ExecutePostAsync<TpLinkData>(req);
                // tips: using system serializer with restsharp isn't working aswell
                // workaround: use system/.net core default serializer manually
                // look like i'm using the wrong attribute in properties: system json instead of restsharp's https://github.com/restsharp/RestSharp/wiki/Serialization

                //var response = await restClient.ExecuteTaskAsync/*<TpLinkData<SystemLog>>*/(sysLogRequest);
                //var data = System.Text.Json.JsonSerializer.Deserialize<TpLinkData<SystemLog>>(response.Content, jsonOptions);
                //var response = await restClient.ExecuteAsync<object>(powerLineStatusRequest);
                //var data = System.Text.Json.JsonSerializer.Deserialize<TpLinkClientData>(response.Content, jsonOptions);
                //_logger.LogInformation(data.Datas.Last().ToString());
                var res = await _tpLinkClient.GetClientsAsync();
                var res2 = await _tpLinkClient.WifiMoveAsync(true);

                //_logger.LogInformation(result.Data.FirstOrDefault().DeviceName);
                _logger.LogInformation("reading system log");
                foreach (var item in (await _tpLinkClient.GetSystemLogsAsync()).Data)
                {
                    Console.WriteLine(item.ToString());
                }
                await Task.Delay(1000 * 10, stoppingToken);
                //Console.Clear();
            }
        }

        private async Task<int> ShowCLients()
        {
            var tpLinkClients = await _tpLinkClient.GetCountConnectedClientsAsync();
            foreach (var client in (await _tpLinkClient.GetClientsAsync()).Data)
            {
                _logger.LogInformation(client.ToString());
            }
            return tpLinkClients;
        }

    }
}
