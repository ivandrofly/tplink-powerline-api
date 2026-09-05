using System;
using System.Threading.Tasks;
using TpLink.Api;

namespace TpLink.Cli.Commands
{
    public class TurnOffSignal : ICommand
    {
        public async Task Execute(ITpLinkClient powerLine)
        {
            Console.WriteLine("turning off 5ghz and 2.4ghz signal!");
            await powerLine.ChangeWireless5GStatusAsync(false);
            await powerLine.ChangeWireless2GStatusAsync(false);
        }
    }
}
