using System;
using System.Threading.Tasks;
using TpLink.Api;

namespace TpLink.Cli.Commands
{
    public class RebootCommand : ICommand
    {
        public async Task Execute(ITpLinkClient powerLine)
        {
            Console.WriteLine("Rebooting..");
            await powerLine.RebootAsync();
            Console.WriteLine("Reboot requested; the adapter drops the connection while it restarts.");
        }
    }
}
