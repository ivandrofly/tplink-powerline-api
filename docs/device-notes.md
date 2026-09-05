# Device notes: TP-Link TL-WPA8630P

Working notes gathered while reverse-engineering the adapter's web-admin interface. They used to live as
commented-out experiments inside `TpLinkClient.cs` and `Worker.cs`; the code they refer to is gone, the knowledge
is kept here. Add to this file when you capture a new form post.

## Protocol basics

- Every operation is `POST /admin/<page>?form=<form>` with `operation=load|read|write|insert` and the other fields
  url-encoded in the body. The web UI always carries `form` in the query string; the device also accepted it in
  the body (the client list did that for years).
- Responses are JSON labelled `Content-Type: text/html`, so RestSharp's typed `ExecuteAsync<T>` picks the wrong
  serializer. Every attempt to fix that from the RestSharp side failed:
  - the default (Newtonsoft-based) serializer in old RestSharp returned nulls;
  - `RestSharp.Serializers.SystemTextJson` (`UseSystemTextJson`) did not help either, partly because the models use
    `System.Text.Json` attributes rather than RestSharp's;
  - `AddHandler("text/html", ...)` and `RemoveHandler` threw.
  The workaround that stuck: `ExecuteAsync` for the raw body, then `System.Text.Json` by hand (`SendAsync<T>`).
- The device allows one admin session. While the web manager is open in a browser every request answers
  `{"success":false,...}` with HTTP 200 (`timeout` may be `true`).
- Authentication is a cookie: `Cookie: Authorization=Basic%20{login}%3A{md5(password)}` (URL-escaped) on every
  request, exactly what the browser sends after login. The other default headers (Accept, X-Requested-With,
  Referer, Origin, DNT, ...) mimic Chrome 80; the user agent is the one the capture was made with. A user-agent
  rewrite rule in Fiddler overrides it and produces confusing failures.

## Radios (`admin/wireless?form=wireless_2g|wireless_5g`)

- `operation=read` returns the full model: `enable`, `ssid`, `hidden`, `encryption`, `psk_version`, `psk_cipher`,
  `psk_key`, `wep_*`, `hwmode`, `htmode`, `channel`, `txpower`, `disabled`, `wireless_2g_disabled*`.
  `operation=write` expects the same fields back.
- The first 5 GHz toggle read only `wireless_5g_ssid`, `wireless_5g_pwd` and `wireless_5g_encryption` from
  `admin/wlan_status` and hard-coded `hidden=off`, `psk_version=auto`, `psk_cipher=auto`, `hwmode=a`, `htmode=80`,
  `channel=auto`, `txpower=low`. That reset a hidden SSID, a fixed channel or the transmit power on every call.
  Always read, mutate, write back.
- `RestRequest.AddObject(dictionary)` throws; flatten the model yourself (`TpLinkClient.AddFormFields`).
- Experiments that poked at `data.enable` directly (Newtonsoft `JObject.SelectToken("data")`,
  `JsonDocument.GetProperty`, `Deserialize<dynamic>`) were abandoned in favour of deserializing the whole model.

## Wi-Fi schedule (`admin/wlanTimeControl`, `operation=insert`)

- Body: `key=add`, `old=add`, `index=0`,
  `new={"stime":"20","etime":"23","days":"127","week_sun":"1",...,"week_sat":"1","enable":"on"}`.
- `days` is a bitmask (Sunday = 1, Monday = 2, ... Saturday = 64); the `week_*` flags repeat it bit by bit.
- Open question: `index` probably should come from sorting the existing rules by start time. `0` works.

## Wi-Fi Move (`admin/wifiMove.json`, `operation=write`, `enable=1|0`)

- Not working: the request matches the browser capture but the device does not apply it. Next diagnostic: diff
  the browser request and ours byte for byte (User-Agent, Accept, a second cookie the device may set via
  Set-Cookie).

## Reboot (`admin/reboot.json`, `operation=write`)

- The device drops the connection while rebooting, so there is often no body; treat the call as fire-and-forget.
- A scheduled radio-off state survives a reboot; the radio does not come back on by itself.

## Guest networks (`admin/guest?form=guest_2g|guest_5g`, `operation=read`)

- Same shape for both bands with a `guest_2g_` / `guest_5g_` prefix on every field: `enable`, `disabled`,
  `hidden`, `ssid`, `psk_key`, `encryption`. The client reads both with one `GuestNetwork` model and a
  per-band naming policy.
- A 3 s timeout was once added here on the theory that a disabled guest network makes the device hang;
  unconfirmed.

## Powerline peers (`admin/powerline?form=plc_device`, `operation=load`)

- Returns `device_mac`, `device_password` (the powerline network key: do not log it), `rx_rate`, `tx_rate`,
  `status`.

## Connected clients (`admin/wireless?form=statistics`, `operation=load`)

- Returns `mac`, `type`, `encryption`, `rxpkts`, `txpkts`, `ip`, `devName` per client plus a top-level
  `max_rules`. Clients without a DHCP lease can report an empty `ip`.

## System log (`admin/syslog?form=log`, `operation=load`)

- Returns `time`, `type`, `level`, `content` as strings.

## Discovery (UDP)

- Broadcast to `255.255.255.255:1040`; the answer arrives on local port `61000` and its source address is the
  adapter.
- Wireshark filter used for the capture:
  `(ip.dst == 192.168.1.108 && ip.src == 192.168.1.86) || (ip.dst == 255.255.255.255)`
  (the adapter was `192.168.1.86` at the time; the address is DHCP-assigned and changes).
- Original discover datagram from the capture:
  `02 03 01 00 00 00 00 00 e8 03 12 00 75 7c be 42 dc 81 21 f6 e1 5e ff c0 c4 1e 25 96`.
  The plain text `Where are you!` gets the same answer and is what the library sends.
- Does not work through a VPN (the broadcast leaves on the wrong interface); with several adapters the first
  answer wins. Check the active interface under Control Panel > Network and Internet > Network Connections when
  in doubt.
- An earlier helper enumerated `NetworkInterface.GetAllNetworkInterfaces()` to pick the Wi-Fi or Ethernet
  address and bind the socket to it (VirtualBox adapters show up there too). Binding to `IPAddress.Any` made it
  unnecessary.

## Firmware

- Only the plaintext firmware is supported. Newer builds (v2.x, 2019 onwards) fetch an RSA key from
  `login?form=keys`, AES-encrypt bodies and answer `{"data":"<base64>"}`. See the README section
  "Firmware compatibility" and issue #5.
