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

// Find the adapter on the LAN with a UDP broadcast, or hardcode its IP.
string ip = await TpLinkClient.DiscoveryAsync();
ITpLinkClient client = new TpLinkClient("admin", "your-password", $"http://{ip}");

// Wi-Fi clients currently connected to the adapter
var clients = await client.GetClientsAsync();
foreach (var c in clients.Data)
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
the adapter answers with HTTP 200 even when it rejects a request (for example while another session is open).

Authentication is a cookie: the client sends `Cookie: Authorization=Basic {login}:{md5(password)}`
on every request, exactly as the browser does after login.

## Supported operations

All members of `ITpLinkClient`, with their status on the TL-WPA8630P:

| Method | Status | Notes |
| --- | --- | --- |
| `GetClientsAsync()` | Works | Connected Wi-Fi clients: name, IP, MAC, packets. |
| `GetCountConnectedClientsAsync()` | Works | Convenience wrapper over the above. |
| `GetPowerlineDevicesStatusAsync()` | Works | Powerline peers with RX/TX rate. |
| `GetSystemLogsAsync()` | Works | Device system log. |
| `GetGuest2GhzAsync()` / `GetGuest5GhzAsync()` | Works | Reads guest network settings. |
| `ChangeWireless5GStatusAsync(bool)` | Works | Turns the 5 GHz radio on or off. |
| `ChangeWireless2GStatusAsync(bool)` | Partial | Posts the change, but returns `null` instead of the updated model. |
| `RebootAsync()` | Works | Fire-and-forget; always returns `Data = true`. |
| `AddNewWifiScheduleAsync(WifiSchedule)` | Works | Inserts the rule at index 0. |
| `WifiMoveAsync(bool)` | Not working | The request matches the browser's but the device does not apply it. |
| `AddNewUserAsync()` | Not implemented | Throws `NotImplementedException`. |
| `AddMacFilterAsync` / `RemoveMacFilterAsync` / `ChangeMacFilterStateAsync` / `GetMacFilterGetDevicesAsync` | Not implemented | Throw `NotImplementedException`. |

`TpLinkClient` also exposes `GetWirelessBand2GAsync()` (reads the 2.4 GHz settings) and the static
`DiscoveryAsync()`, which are not on the interface.

## Discovery

`TpLinkClient.DiscoveryAsync()` broadcasts a UDP packet to port `1040`, listens on port `61000`, and returns the IP of
the first adapter that answers. Known limitations:

- It does not work while traffic is tunnelled through a VPN.
- With several adapters on the LAN it returns whichever answers first.
- A local firewall must allow inbound UDP on port `61000`.

If discovery is unreliable in your setup, skip it and pass the adapter's IP to the constructor directly.

## Sample apps

Two small host apps exercise the library. Both read credentials from two **user-scoped** environment variables:

| Variable | Value |
| --- | --- |
| `tplink_powerline_login` | Web-admin login (usually `admin`) |
| `tplink_powerline_pwd` | Web-admin password |

On Windows set them with `setx tplink_powerline_login admin` and `setx tplink_powerline_pwd <password>`
(open a new terminal afterwards). .NET only supports user-scoped variables on Windows; on Linux or macOS
pass the credentials straight to the `TpLinkClient` constructor instead.

- **`Client.Console`**: `dotnet run --project Client.Console`. Discovers the adapter, then runs whichever
  command is uncommented in `Client.Console/Program.cs` (turn both radios on or off, reboot, list connected clients).
  Edit `Program.cs` to pick a different action.
- **`TpLink.Service`**: `dotnet run --project TpLinkDataRate/TpLink.Service.csproj`. A generic-host
  `BackgroundService` that discovers the adapter, enables the 5 GHz radio and prints the powerline peer status.

## Troubleshooting

- **Requests fail or `Success` is `false`**: close the adapter's web manager in your browser. The device allows a
  single admin session, and the browser holds it.
- **Discovery hangs or times out**: disconnect from any VPN, check the firewall rule for UDP `61000`, or pass the IP manually.
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
