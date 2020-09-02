using System.Collections.Generic;
using System.Threading.Tasks;
using TpLink.Api.Models;
using TpLink.Models;

namespace TpLink.Api
{
    public interface ITpLinkClient
    {
        Task<TpLinkClientData> GetClientsAsync();
        Task<TpLinkResponse<PowerlineDevices>> GetPowerlineDevicesStatusAsync();
        Task<TpLinkResponse<List<SystemLog>>> GetSystemLogsAsync();
        Task<int> GetCountConnectedClientsAsync();
        Task<TpLinkResponse<WirelessModel>> ChangeWireless2GStatusAsync(bool enabled);
        Task<TpLinkResponse<WirelessModel>> ChangeWireless5GStatusAsync(bool enabled);
        Task<TpLinkResponse<WifiMove>> WifiMoveAsync(bool enabled);
        Task<TpLinkResponse<bool>> RebootAsync();
    }
}