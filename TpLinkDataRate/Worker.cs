using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TpLink.Api;

namespace TpLink.Service
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
            //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
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

                // test wifi-move
                // var res2 = await _tpLinkClient.WifiMoveAsync(true);
                // turn off radios for 5ghz network

                var res = await _tpLinkClient.ChangeWireless5GStatusAsync(true);

                var response = await _tpLinkClient.GetPowerlineDevicesStatusAsync();
                if (response.Success)
                {
                    Console.WriteLine($"password: {response.Data.First().Password}");
                    Console.WriteLine("operation success");
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("operation failed (make sure you don't have powerline openned in browser)");
                }

                Console.WriteLine("done ");
                // test reboot
                // note: the turn off is scheduled, powerline won't turn the wi-fi one by default
                //await _tpLinkClient.RebootAsync();

                // test system log
                //_logger.LogInformation(result.Data.FirstOrDefault().DeviceName);
                //_logger.LogInformation("reading system log");
                //foreach (var item in (await _tpLinkClient.GetSystemLogsAsync()).Data)
                //{
                //    Console.WriteLine(item.ToString());
                //}

                await Task.Delay(1000 * 5, stoppingToken);
                Console.Clear();
                break;
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
