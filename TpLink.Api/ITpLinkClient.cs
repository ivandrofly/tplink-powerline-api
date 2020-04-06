using System.Threading.Tasks;
using TpLink.Models;

namespace TpLink.Api
{
    public interface ITpLinkClient
    {
        Task<TpLinkClientData> GetClientsAsync();
        Task<TpLinkData<SystemLog>> GetPowerlineDevicesStatusAsync();
        Task<TpLinkData<SystemLog>> GetSystemLogsAsync();
        Task<int> GetCountConnectedClientsAsync();
    }
}