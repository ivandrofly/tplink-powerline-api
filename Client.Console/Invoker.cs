using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Console.Commands;
using TpLink.Api;

namespace Client.Console
{
    public class Invoker
    {
        private readonly ICommand turnOnCommand;
        private readonly ICommand turnOffCommand;
        private readonly ICommand rebootCommand;
        private readonly ICommand printCommand;
        private ITpLinkClient powerLine;

        public Invoker()
        {
            //System.Console.WriteLine("Discovering...");
            //var commands = new List<ICommand> {new DisplayConnectedCommand(), new TurnOffSignal()};
            turnOnCommand = new TurnOnSignal();
            turnOffCommand = new TurnOffSignal();
            rebootCommand = new RebootCommand();
            printCommand = new DisplayConnectedCommand();

            //DiscoverAsync().GetAwaiter().GetResult();
        }

        public async Task DiscoverAsync()
        {
            string login = Environment.GetEnvironmentVariable("tplink_powerline_login", EnvironmentVariableTarget.User);
            string pwd = Environment.GetEnvironmentVariable("tplink_powerline_pwd", EnvironmentVariableTarget.User);
            string endpoint = await TpLinkClient.DiscoveryAsync();
            powerLine = new TpLinkClient(login, pwd, $"http://{endpoint}");
        }

        public Task TurnOn()
        {
            System.Console.WriteLine("turning on 5ghz and 2.4ghz");
            // Task.WhenAll(new[]
            // {
            //     turnOnCommand.Execute(powerLine),
            //     
            // });
            return turnOnCommand.Execute(powerLine);
        }

        public Task TurnOff()
        {
            System.Console.WriteLine("turning off 5ghz and 2.4ghz");
            return turnOffCommand.Execute(powerLine);
        }

        public Task Reboot()
        {
            System.Console.WriteLine("rebooting..");
            return rebootCommand.Execute(powerLine);
        }

        public async Task RunBatch()
        {
            System.Console.WriteLine("running batch commands");
            await printCommand.Execute(powerLine);
            await rebootCommand.Execute(powerLine);
            await Task.Delay(1000 * 60);
            await printCommand.Execute(powerLine);
        }

        public Task DisplayClient()
        {
            return printCommand.Execute(powerLine);
        }


        // await Task.Delay(1000 * 60);
        //commands.ForEach(c => c.Execute(powerLine));
        //await Task.Delay(1000 * 60);
        //var turnOnSign = new TurnOnSignal();
        //await turnOnSign.Execute(powerLine);

    }
}
