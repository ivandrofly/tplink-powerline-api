namespace TpLink.Api;

/// <summary>
/// Finds the powerline adapter on the LAN. Hosts inject it so tests can substitute a fake instead of
/// broadcasting on the network.
/// </summary>
public interface ITpLinkDiscovery
{
    /// <returns>The adapter's IP address, e.g. <c>192.168.1.86</c>.</returns>
    /// <exception cref="TimeoutException">No adapter answered within <paramref name="timeout"/>.</exception>
    Task<string> DiscoverAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}
