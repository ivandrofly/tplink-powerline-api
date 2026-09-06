using System.Threading.Tasks;
using TpLink.Api;

namespace TpLink.Cli.Commands
{
    public interface ICommand
    {
        Task Execute(ITpLinkClient powerLine);
    }
}
