using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TpLink.Api;

namespace Client.Console.Commands
{
    public class DisplayConnectedCommand : ICommand
    {
        public async Task Execute(ITpLinkClient powerLine)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Execuding display command");
            var result = await powerLine.GetClientsAsync();
            if (result.Data.Count < 1)
            {
                return;
            }

            System.Console.WriteLine();
            System.Console.WriteLine($"{"Name",-30}|{"IP",-30}|Mac");
            // foreach (var client in result.Data.OrderBy(c => int.Parse(c.IP[^2..])))
            foreach (var client in result.Data.OrderBy(c => int.Parse(c.IP.Split('.').Last())))
            {
                System.Console.WriteLine($"{client.DeviceName,-30}|{client.IP,-30}|{client.Mac.Replace("-", ":")}");
            }
        }
    }
}