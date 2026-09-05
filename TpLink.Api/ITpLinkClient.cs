using TpLink.Api.Models;

namespace TpLink.Api;

/// <summary>
/// Client for the web-admin interface of a TP-Link powerline adapter.
/// </summary>
/// <remarks>
/// Every call returns a <see cref="TpLinkResponse{TData}"/>; check <c>Success</c>, because the device answers
/// HTTP 200 with <c>Success == false</c> and <c>Data == null</c> when it rejects a request (typically while its
/// web manager is open in a browser). Transport failures throw <see cref="TpLinkException"/>.
/// </remarks>
public interface ITpLinkClient : IDisposable
{
    /// <summary>The adapter's base URL, e.g. <c>http://192.168.1.86</c>.</summary>
    string Endpoint { get; }

    /// <summary>Wireless clients currently connected to the adapter (name, IP, MAC, packet counters).</summary>
    Task<TpLinkClientData> GetClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>Number of wireless clients currently connected; 0 when the device rejects the request.</summary>
    Task<int> GetConnectedClientCountAsync(CancellationToken cancellationToken = default);

    /// <summary>Powerline peers with their receive and transmit link rates.</summary>
    Task<TpLinkResponse<IList<Device>>> GetPowerlineDevicesStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>The device's system log.</summary>
    Task<TpLinkResponse<List<SystemLog>>> GetSystemLogsAsync(CancellationToken cancellationToken = default);

    /// <summary>Current 2.4 GHz radio settings.</summary>
    Task<TpLinkResponse<WirelessModel>> GetWirelessBand2GAsync(CancellationToken cancellationToken = default);

    /// <summary>Current 5 GHz radio settings.</summary>
    Task<TpLinkResponse<WirelessModel>> GetWirelessBand5GAsync(CancellationToken cancellationToken = default);

    /// <summary>Turn the 2.4 GHz radio on or off, leaving every other setting untouched.</summary>
    Task<TpLinkResponse<WirelessModel>> ChangeWireless2GStatusAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>Turn the 5 GHz radio on or off, leaving every other setting untouched.</summary>
    Task<TpLinkResponse<WirelessModel>> ChangeWireless5GStatusAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>Guest network settings for the 2.4 GHz band.</summary>
    Task<TpLinkResponse<GuestNetwork>> GetGuest2GAsync(CancellationToken cancellationToken = default);

    /// <summary>Guest network settings for the 5 GHz band.</summary>
    Task<TpLinkResponse<GuestNetwork>> GetGuest5GAsync(CancellationToken cancellationToken = default);

    /// <summary>Insert a Wi-Fi schedule rule (radio off between the given hours on the given days).</summary>
    /// <exception cref="ArgumentOutOfRangeException">An hour is outside 0-24.</exception>
    /// <exception cref="ArgumentException">Start is not earlier than end.</exception>
    Task<TpLinkResponse<ICollection<WifiSchedule>>> AddNewWifiScheduleAsync(WifiSchedule wifiSchedule, CancellationToken cancellationToken = default);

    /// <summary>Enable or disable Wi-Fi Move. Known not to take effect on the TL-WPA8630P even though the request matches the browser's.</summary>
    Task<TpLinkResponse<WifiMove>> SetWifiMoveAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>Reboot the adapter. Fire-and-forget: the device drops the connection, so the result is always <c>Data == true</c>.</summary>
    Task<TpLinkResponse<bool>> RebootAsync(CancellationToken cancellationToken = default);
}
