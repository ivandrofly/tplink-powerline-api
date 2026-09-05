# tplink-powerline-api

A .NET client library (`TpLink.Api`) for the **TP-Link TL-WPA8630P** powerline Wi-Fi extender.
It drives the adapter's web-admin interface by replaying the same HTTP form posts the browser UI makes,
so anything you can do from `http://<adapter-ip>/` in a browser is a candidate for a method here.

It has only been tested on the TL-WPA8630P. Other TP-Link powerline models that share the same
web UI may work, but nothing else has been verified. Contributions and reports for other models are welcome.

![tl-wpa8630p](https://static.tp-link.com/TL-WPA8630P_KIT(EU)2.0_01_590%EF%80%A1590_1501232945861v.jpg)

## Firmware compatibility

The library speaks the **original plaintext protocol**: every request is an
`application/x-www-form-urlencoded` post and every response is plain JSON.

Newer TP-Link firmware (for the TL-WPA8630P v2.x, builds from around 2019 onwards, e.g.
`2.1.1 Build 20220605`) encrypts the web-admin traffic. It fetches an RSA key from `login?form=keys`,
AES-encrypts request bodies, and returns responses shaped like `{"data":"<base64 blob>"}`.
**That firmware is not supported yet.** If your adapter returns an opaque base64 `data` string instead of
JSON objects, this is why. See [issue #5](https://github.com/ivandrofly/tplink-powerline-api/issues/5)
for details and pointers to implementations of the encrypted handshake.

## Requirements

- .NET SDK **9.0.200 or newer** to build (the solution file is the XML `.slnx` format). Projects target `net8.0`.
- The adapter on the same LAN as the machine running the code, reachable over HTTP.
- The adapter's web-admin login and password.

## Build and test

```
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~StringUtilsTest.WifiScheduleTest"
```

CI (`.github/workflows/dotnet.yml`) runs restore, build and test on the .NET 9.0.x SDK.

## Quick start

Reference the `TpLink.Api` project (there is no NuGet package yet), then:

```csharp
using TpLink.Api;

// Discovers the adapter on the LAN with a UDP broadcast; set Endpoint = "http://<ip>" to skip discovery.
// The client owns an HTTP connection, so dispose it when you are done.
using var client = await TpLinkClient.CreateAsync(new TpLinkOptions
{
    Login = "admin",
    Password = "your-password",
});

// Or, with a known address: new TpLinkClient("admin", "your-password", "http://192.168.1.86")

// Wi-Fi clients currently connected to the adapter
var clients = await client.GetClientsAsync();
foreach (var c in clients.Data ?? [])
    Console.WriteLine($"{c.DeviceName} {c.IP} {c.Mac}");

// Powerline peers and their link rates
var powerline = await client.GetPowerlineDevicesStatusAsync();
if (powerline.Success)
    foreach (var d in powerline.Data)
        Console.WriteLine($"{d.Mac} rx={d.RXRate} tx={d.TXRate}");

// Turn the 5 GHz radio off, then reboot the adapter
await client.ChangeWireless5GStatusAsync(false);
await client.RebootAsync();
```

Adding a Wi-Fi schedule (radio off between the given hours on the given days):

```csharp
using TpLink.Api.Models;

var schedule = new WifiSchedule
{
    Enable = true,
    StartTime = 22,   // hour of day, 0-24; must be less than EndTime
    EndTime = 24,
    Days = Days.Monday | Days.Tuesday | Days.Wednesday | Days.Thursday | Days.Friday
};

await client.AddNewWifiScheduleAsync(schedule);
```

Every call returns a `TpLinkResponse<T>` with `Success`, `Timeout` and `Data`. Always check `Success`:
the adapter answers with HTTP 200 even when it rejects a request (for example while another session is open),
and `Data` is `null` in that case.

If the request never reaches the device (connection refused, timeout, a non-2xx status or an empty body) the
client throws `TpLinkException`, which carries `StatusCode` and `TimedOut`. A body that is not the expected JSON
throws `JsonException`. Requests time out after 10 seconds by default (`TpLinkOptions.RequestTimeout` or the
`TpLinkClient(EndpointAuth, TimeSpan)` constructor change that), and every method takes an optional `CancellationToken`.

Authentication is a cookie: the client sends `Cookie: Authorization=Basic {login}:{md5(password)}`
on every request, exactly as the browser does after login.

## Supported operations

All members of `ITpLinkClient`, with their status on the TL-WPA8630P:

| Method | Status | Notes |
| --- | --- | --- |
| `GetClientsAsync()` | Works | Connected Wi-Fi clients: name, IP, MAC, packets. |
| `GetConnectedClientCountAsync()` | Works | Convenience wrapper over the above; 0 when the device rejects the request. |
| `GetPowerlineDevicesStatusAsync()` | Works | Powerline peers with RX/TX rate. |
| `GetSystemLogsAsync()` | Works | Device system log. |
| `GetWirelessBand2GAsync()` / `GetWirelessBand5GAsync()` | Works | Reads the radio settings. |
| `GetGuest2GAsync()` / `GetGuest5GAsync()` | Works | Reads guest network settings. |
| `ChangeWireless5GStatusAsync(bool)` | Works | Turns the 5 GHz radio on or off. Reads the current settings first and posts them back unchanged. |
| `ChangeWireless2GStatusAsync(bool)` | Works | Same as above for the 2.4 GHz radio. |
| `RebootAsync()` | Works | Fire-and-forget; always returns `Data = true`. |
| `AddNewWifiScheduleAsync(WifiSchedule)` | Works | Inserts the rule at index 0. |
| `SetWifiMoveAsync(bool)` | Not working | The request matches the browser's but the device does not apply it. |

Every member takes an optional `CancellationToken`, and `ITpLinkClient` is `IDisposable`. The static
`TpLinkClient.DiscoveryAsync()` and `TpLinkClient.CreateAsync(TpLinkOptions)` are not on the interface.
User management and MAC filtering are not implemented; the earlier placeholder members that threw
`NotImplementedException` were removed, and contributions that capture those form posts are welcome.

## Discovery

`TpLinkClient.DiscoveryAsync()` broadcasts a UDP packet to port `1040`, listens on port `61000`, and returns the IP of
the first adapter that answers. It waits 5 seconds by default and throws `TimeoutException` if nothing answers;
`DiscoveryAsync(TimeSpan timeout, CancellationToken cancellationToken)` lets you change the wait or cancel it.
Known limitations:

- It does not work while traffic is tunnelled through a VPN.
- With several adapters on the LAN it returns whichever answers first.
- A local firewall must allow inbound UDP on port `61000`.

If discovery is unreliable in your setup, skip it and pass the adapter's IP to the constructor directly.

## Sample apps

Two small host apps exercise the library. Both build a `TpLinkOptions` and read anything missing from these
environment variables:

| Variable | Value |
| --- | --- |
| `tplink_powerline_login` | Web-admin login (usually `admin`) |
| `tplink_powerline_pwd` | Web-admin password |
| `tplink_powerline_endpoint` | Optional adapter URL such as `http://192.168.1.86`; skips discovery when set |

On Windows set them once with `setx tplink_powerline_login admin` and `setx tplink_powerline_pwd <password>`
and open a new terminal, or set them for the current session with `$env:tplink_powerline_login = "admin"`.
On Linux or macOS use `export tplink_powerline_login=admin`. Both apps fail fast with a message naming the
configuration keys and variables when the login or password is missing.

- **`Client.Console`**: `dotnet run --project Client.Console`. Discovers the adapter, then runs whichever
  command is uncommented in `Client.Console/Program.cs` (turn both radios on or off, reboot, list connected clients).
  Edit `Program.cs` to pick a different action.
- **`TpLink.Service`**: `dotnet run --project TpLinkDataRate/TpLink.Service.csproj`. A generic-host
  `BackgroundService` that connects to the adapter and logs the link rate of every powerline peer every
  `Worker:PollInterval` (5 seconds by default, see `appsettings.json`). It also binds the `TpLink` configuration
  section, so the credentials can come from user-secrets (`dotnet user-secrets set TpLink:Password <password>`
  inside `TpLinkDataRate/`), from `appsettings.json`, or from `TpLink__Login`-style environment variables.

## Troubleshooting

- **Requests fail or `Success` is `false`**: close the adapter's web manager in your browser. The device allows a
  single admin session, and the browser holds it.
- **Discovery throws `TimeoutException`**: disconnect from any VPN, check the firewall rule for UDP `61000`, or pass the IP manually.
- **`TpLinkException` on every call**: the adapter is unreachable at that address (wrong IP, powered off, or the
  request timed out; check `TimedOut` and the inner exception).
- **`JsonException` on a response with a base64 `data` string**: your firmware encrypts the web-admin traffic.
  See [Firmware compatibility](#firmware-compatibility).
- **Responses arrive as `text/html`**: expected. The adapter labels its JSON as HTML, which is why the library
  deserializes with `System.Text.Json` itself instead of RestSharp's typed `ExecuteAsync<T>`.

## Contributing

Pull requests are welcome, especially reports and fixes for other TP-Link powerline models or for the
encrypted firmware. The easiest way to add an operation is to capture the form post the web UI sends
(browser dev tools or Wireshark) and replay it the way the existing methods in `TpLink.Api/TpLinkClient.cs` do.

## License

GPL-3.0. See [LICENSE](LICENSE).
