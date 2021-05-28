using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TpLink.Api;

namespace Client.Console.Commands
{
    class TurnOnSignal : ICommand
    {
        public async Task Execute(ITpLinkClient powerLine)
        {
            await powerLine.ChangeWireless5GStatusAsync(true);
            await powerLine.ChangeWireless2GStatusAsync(true);
        }
    }
}
