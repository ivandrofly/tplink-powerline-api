using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TpLink.Api;

namespace TpLink.Service
{
    /// <summary>
    /// Connects to the powerline adapter (discovering it when no endpoint is configured) and logs the link rate
    /// of every powerline peer on a fixed interval until the host stops.
    /// </summary>
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TpLinkOptions _tpLinkOptions;
        private readonly WorkerOptions _workerOptions;

        public Worker(ILogger<Worker> logger, IOptions<TpLinkOptions> tpLinkOptions, IOptions<WorkerOptions> workerOptions)
        {
            _logger = logger;
            _tpLinkOptions = tpLinkOptions.Value;
            _workerOptions = workerOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_tpLinkOptions.Endpoint))
                {
                    _logger.LogInformation("Discovering the powerline adapter (disable any VPN first)");
                }

                using var client = await TpLinkClient.CreateAsync(_tpLinkOptions, stoppingToken);
                _logger.LogInformation("Polling {Endpoint} every {Interval}", client.Endpoint, _workerOptions.PollInterval);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await PollAsync(client, stoppingToken);
                    await Task.Delay(_workerOptions.PollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // host shutdown
            }
        }

        private async Task PollAsync(ITpLinkClient client, CancellationToken cancellationToken)
        {
            try
            {
                var status = await client.GetPowerlineDevicesStatusAsync(cancellationToken);
                if (!status.Success || status.Data == null)
                {
                    _logger.LogWarning("The adapter rejected the request; close its web manager in the browser and it will recover");
                    return;
                }

                foreach (var device in status.Data)
                {
                    _logger.LogInformation("{Mac} {Status}: rx {RxRate}, tx {TxRate}", device.Mac, device.Status, device.RXRate, device.TXRate);
                }
            }
            catch (TpLinkException ex)
            {
                _logger.LogError(ex, "Request to the adapter failed");
            }
        }
    }
}
