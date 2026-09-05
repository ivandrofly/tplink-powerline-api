using System;
using System.Threading.Tasks;
using Client.Console.Commands;
using TpLink.Api;

namespace Client.Console
{
    public class Invoker : IDisposable
    {
        private readonly ICommand turnOnCommand;
        private readonly ICommand turnOffCommand;
        private readonly ICommand rebootCommand;
        private readonly ICommand printCommand;
        private ITpLinkClient powerLine;

        public Invoker()
        {
            turnOnCommand = new TurnOnSignal();
            turnOffCommand = new TurnOffSignal();
            rebootCommand = new RebootCommand();
            printCommand = new DisplayConnectedCommand();
        }

        /// <summary>
        /// Read the credentials (and optional endpoint) from the tplink_powerline_* environment variables and
        /// discover the adapter on the LAN unless an endpoint was given.
        /// </summary>
        public async Task DiscoverAsync()
        {
            var options = new TpLinkOptions().ApplyEnvironmentFallback();
            if (string.IsNullOrWhiteSpace(options.Endpoint))
            {
                System.Console.WriteLine("Discovering the powerline adapter...");
            }

            powerLine = await TpLinkClient.CreateAsync(options);
            System.Console.WriteLine($"Using adapter at {powerLine.Endpoint}");
        }

        public Task TurnOn()
        {
            System.Console.WriteLine("turning on 5ghz and 2.4ghz");
            return turnOnCommand.Execute(Client);
        }

        public Task TurnOff()
        {
            System.Console.WriteLine("turning off 5ghz and 2.4ghz");
            return turnOffCommand.Execute(Client);
        }

        public Task Reboot()
        {
            System.Console.WriteLine("rebooting..");
            return rebootCommand.Execute(Client);
        }

        public async Task RunBatch()
        {
            System.Console.WriteLine("running batch commands");
            await printCommand.Execute(Client);
            await rebootCommand.Execute(Client);
            await Task.Delay(1000 * 60);
            await printCommand.Execute(Client);
        }

        public Task DisplayClient()
        {
            return printCommand.Execute(Client);
        }

        public void Dispose() => powerLine?.Dispose();

        private ITpLinkClient Client =>
            powerLine ?? throw new InvalidOperationException($"Call {nameof(DiscoverAsync)} before running a command.");
    }
}
