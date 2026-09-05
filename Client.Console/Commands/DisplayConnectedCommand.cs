using System.Linq;
using System.Threading.Tasks;
using TpLink.Api;

namespace Client.Console.Commands
{
    public class DisplayConnectedCommand : ICommand
    {
        public async Task Execute(ITpLinkClient powerLine)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Executing display command");
            var result = await powerLine.GetClientsAsync();
            if (!result.Success || result.Data == null)
            {
                System.Console.WriteLine("The adapter rejected the request (make sure its web manager is not open in a browser).");
                return;
            }

            if (result.Data.Count == 0)
            {
                System.Console.WriteLine("No wireless clients connected.");
                return;
            }

            System.Console.WriteLine();
            System.Console.WriteLine($"{"Name",-30}|{"IP",-30}|Mac");
            foreach (var client in result.Data.OrderBy(c => LastOctet(c.IP)))
            {
                System.Console.WriteLine($"{client.DeviceName,-30}|{client.IP,-30}|{client.Mac?.Replace("-", ":")}");
            }
        }

        // clients without a lease report no IP; sort those last instead of throwing on int.Parse
        private static int LastOctet(string ip) =>
            ip != null && int.TryParse(ip.Split('.').Last(), out var octet) ? octet : int.MaxValue;
    }
}
