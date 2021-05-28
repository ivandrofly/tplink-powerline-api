using System.Threading.Tasks;
using TpLink.Api;

namespace Client.Console.Commands
{
    public interface ICommand
    {
        Task Execute(ITpLinkClient powerLine);
    }
}
