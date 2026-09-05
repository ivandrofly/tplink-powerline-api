# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET 8 client library (`TpLink.Api`) that drives the web-admin interface of a TP-Link TL-WPA8630P powerline adapter by replaying the same HTTP form posts the browser UI makes. Tested only on that model. Two small host apps (`Client.Console`, `TpLink.Service`) exercise it, and `TpLink.UnitTest` holds xUnit tests.

## Commands

The solution is `tplink-powerline.slnx` (the XML solution format), so the .NET SDK must be 9.0.200 or newer to build it. Bare `dotnet build` and `dotnet test` from the repo root work.

```
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~StringUtilsTest.WifiScheduleTest"
dotnet run --project Client.Console
dotnet run --project TpLinkDataRate/TpLink.Service.csproj
```

CI (`.github/workflows/dotnet.yml`) runs restore, build, and test on the .NET 9.0.x SDK; projects still target `net8.0`. `Directory.Build.props` pins `net8.0` for every project; `Directory.Packages.props` does central package version management, so add new packages there and reference them without a `Version` in the csproj.

Running either host app needs a live adapter on the LAN plus two user-scoped environment variables: `tplink_powerline_login` and `tplink_powerline_pwd`. The apps locate the adapter with a UDP broadcast (`TpLinkClient.DiscoveryAsync`), which fails through a VPN or when several adapters are up. Requests also fail while the adapter's web manager is open in a browser, because the device allows one session.

## Architecture

**Auth is a cookie, not a header.** `StringUtils.GetAuthorization` builds `Basic {login}:{md5(password)}`, URL-escapes it, and `TpLinkClient` sets it as a default `Cookie: Authorization=...` header on the shared `RestClient`. The unit test pins this exact string, so changing hashing or escaping breaks it.

**Every call is a form post.** Endpoints look like `admin/wireless?form=wireless_2g` with `operation=load|read|write|insert` and other fields as `application/x-www-form-urlencoded` parameters via `RestRequest.AddParameter(..., ParameterType.GetOrPost)`. Nothing is sent as a JSON body, so `JsonPropertyName` attributes and `TpLinkPropertyNamingPolicy` only affect deserialization. When writing settings back (see `ChangeWireless2GStatusAsync`), the client reads the current model, mutates it, then reflects over its properties to emit each one as a form field using the `JsonPropertyName` name.

**Deserialization is manual.** The adapter returns JSON with `Content-Type: text/html`, which makes RestSharp pick the wrong serializer. Every method therefore calls `_apiConnection.ExecuteAsync(req)` and runs `System.Text.Json.JsonSerializer.Deserialize<T>(response.Content, jsonOption)` itself. Keep that pattern rather than switching to `ExecuteAsync<T>`.

**Response envelope.** Responses deserialize into `TpLinkResponse<TData>` with `Success`, `Timeout`, and `Data`. `TpLinkClientData` extends it with `max_rules` for the wireless client list. Device fields arrive as strings ("on"/"off", "1"/"0", numbers as text), so models keep `string` properties or use the converters in `TpLink.Api/Converters` (`StringBoolConverter` for on/off, `BoolToBitConvert` for 1/0, `IntToString`, `DaysEnumToCustomString`). `WifiSchedule` shows the full pattern: a `[Flags] Days` enum serialized as its byte value plus one derived `week_*` bit property per day.

**Host apps.** `Client.Console` uses a command pattern (`ICommand.Execute(ITpLinkClient)` with `Invoker` wiring `TurnOnSignal`, `TurnOffSignal`, `RebootCommand`, `DisplayConnectedCommand`); `Program.cs` is top-level statements with commented-out calls toggled by hand. `TpLinkDataRate` is a generic-host `BackgroundService` that registers `ITpLinkClient` as a singleton after synchronous discovery.

## State of the code

Several `ITpLinkClient` members throw `NotImplementedException` (user and MAC-filter operations). `WifiMoveAsync` is marked not working. `ChangeWireless2GStatusAsync` returns null after posting. Many methods contain commented-out experiments; treat them as notes on what was tried against the device, not dead code to clean up blindly.
