using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TpLink.Api;

/// <summary>
/// Discovers the adapter the way the TP-Link utility does: broadcast a datagram to UDP <see cref="Port"/> and take
/// the source address of the first answer that arrives on local port <see cref="ListenPort"/>.
/// The capture this is based on is in docs/device-notes.md.
/// </summary>
public sealed class UdpTpLinkDiscovery : ITpLinkDiscovery
{
    /// <summary>UDP port the adapter listens on for the discovery broadcast.</summary>
    public const int Port = 1040;

    /// <summary>Local UDP port the discovery reply is received on (open it in the firewall).</summary>
    public const int ListenPort = 61000;

    /// <summary>How long to wait for an adapter to answer by default.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public static UdpTpLinkDiscovery Instance { get; } = new();

    /// <inheritdoc />
    public async Task<string> DiscoverAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var uc = new UdpClient();
        // let a second instance (or a socket lingering from the previous run) bind the same port instead of throwing
        uc.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        uc.Client.Bind(new IPEndPoint(IPAddress.Any, ListenPort));
        uc.EnableBroadcast = true;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            // the captured binary datagram and this plain text get the same answer
            var buffer = Encoding.UTF8.GetBytes("Where are you!");
            await uc.SendAsync(buffer, new IPEndPoint(IPAddress.Broadcast, Port), cts.Token).ConfigureAwait(false);

            // the first datagram that carries a payload wins; empty datagrams are ignored
            while (true)
            {
                var reply = await uc.ReceiveAsync(cts.Token).ConfigureAwait(false);
                if (reply.Buffer.Length > 0)
                {
                    return reply.RemoteEndPoint.Address.ToString();
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"No TP-Link powerline adapter answered the discovery broadcast (UDP {ListenPort} -> {Port}) " +
                $"within {timeout.TotalSeconds:0.#} s. Make sure no VPN is active, the adapter is on this LAN and the firewall " +
                $"allows inbound UDP on port {ListenPort}, or pass the adapter's IP to the constructor instead.");
        }
    }
}
