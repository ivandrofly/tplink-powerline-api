using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TpLink.Api;

namespace Client.Console.Commands
{
    public class TurnOffSignal : ICommand
    {
        public async Task Execute(ITpLinkClient powerLine)
        {
            System.Console.WriteLine("turning off 5ghz and 2.4ghz signal!");
            await powerLine.ChangeWireless5GStatusAsync(false);
            await powerLine.ChangeWireless2GStatusAsync(false);
        }
    }
}
