using System.Collections.Generic;
using System.Threading.Tasks;
using TpLink.Api.Models;
using TpLink.Models;

namespace TpLink.Api
{
    public interface ITpLinkClient
    {
        Task<TpLinkClientData> GetClientsAsync();
        Task<TpLinkData<PowerlineDevices>> GetPowerlineDevicesStatusAsync();
        Task<TpLinkData<List<SystemLog>>> GetSystemLogsAsync();
        Task<int> GetCountConnectedClientsAsync();
        Task<TpLinkData<WirelessModel>> ChangeWireless2GStatusAsync(bool enabled);
        Task<WirelessModel> ChangeWireless5GStatus(bool enabled);
        Task<TpLinkData<WifiMove>> WifiMoveAsync(bool enabled);
    }
}