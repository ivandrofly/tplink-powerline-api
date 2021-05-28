using System.Diagnostics;
using System.Threading.Tasks;
using TpLink.Api;

namespace Client.Console.Commands
{
    public class RebootCommand : ICommand
    {
        public async Task Execute(ITpLinkClient powerLine)
        {
            Debug.WriteLine("Reboting..");
            await powerLine.RebootAsync();
            Debug.WriteLine("done reboot");
        }
    }
}