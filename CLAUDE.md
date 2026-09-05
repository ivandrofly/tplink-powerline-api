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

Running either host app needs a live adapter on the LAN plus two environment variables read from the process environment: `tplink_powerline_login` and `tplink_powerline_pwd` (`setx` or `$env:` on Windows, `export` elsewhere; never `EnvironmentVariableTarget.User`, which returns null on Linux/macOS). The apps locate the adapter with a UDP broadcast (`TpLinkClient.DiscoveryAsync`), which fails through a VPN or when several adapters are up. Requests also fail while the adapter's web manager is open in a browser, because the device allows one session.

## Architecture

**Auth is a cookie, not a header.** `StringUtils.GetAuthorization` builds `Basic {login}:{md5(password)}`, URL-escapes it, and `TpLinkClient` sets it as a default `Cookie: Authorization=...` header on the shared `RestClient`. The unit test pins this exact string, so changing hashing or escaping breaks it.

**Every call is a form post.** Endpoints look like `admin/wireless?form=wireless_2g` with `operation=load|read|write|insert` and other fields as `application/x-www-form-urlencoded` parameters via `RestRequest.AddParameter(..., ParameterType.GetOrPost)`. Nothing is sent as a JSON body. When writing settings back (see the private `ChangeWirelessStatusAsync`, shared by the 2.4 GHz and 5 GHz toggles), the client reads the current model, mutates it, then calls `TpLinkClient.AddFormFields`, which serializes the model with the shared `JsonOptions` and flattens the result into one form field per non-null property. `JsonPropertyName` attributes, the lower-casing `TpLinkPropertyNamingPolicy` and the converters therefore shape both directions. Never post hard-coded values for fields the device already reports; that silently resets user settings.

**Deserialization is manual.** The adapter returns JSON with `Content-Type: text/html`, which makes RestSharp pick the wrong serializer. Every method therefore goes through the private `SendAsync<T>`, which calls `_apiConnection.ExecuteAsync(req)` and runs `System.Text.Json.JsonSerializer.Deserialize<T>(response.Content, JsonOptions)` itself. Keep that pattern rather than switching to `ExecuteAsync<T>`. `RebootAsync` is the one method that bypasses it, because the device drops the connection while rebooting.

**Failure semantics.** RestSharp never throws for transport problems; it leaves `Content` null. `SendAsync<T>` turns that (and any non-2xx or empty response) into a `TpLinkException` with `StatusCode` and `TimedOut`. A request the device accepted but rejected comes back as an envelope with `Success == false` and `Data == null`, so callers must null-check `Data`. Invalid JSON (encrypted firmware) propagates as `JsonException`.

**Response envelope.** Responses deserialize into `TpLinkResponse<TData>` with `Success`, `Timeout`, and `Data`. `TpLinkClientData` extends it with `max_rules` for the wireless client list. Device fields arrive as strings ("on"/"off", "1"/"0", numbers as text), so models keep `string` properties or use the converters in `TpLink.Api/Converters` (`StringBoolConverter` for on/off, `BoolToBitConvert` for 1/0, `IntToString`, `DaysEnumToCustomString`). `WifiSchedule` shows the full pattern: a `[Flags] Days` enum serialized as its byte value plus one derived `week_*` bit property per day.

**Host apps.** `Client.Console` uses a command pattern (`ICommand.Execute(ITpLinkClient)` with `Invoker` wiring `TurnOnSignal`, `TurnOffSignal`, `RebootCommand`, `DisplayConnectedCommand`); `Program.cs` is top-level statements with commented-out calls toggled by hand. `TpLinkDataRate` is a generic-host `BackgroundService` that registers `ITpLinkClient` as a singleton after synchronous discovery.

## State of the code

Several `ITpLinkClient` members throw `NotImplementedException` (user and MAC-filter operations). `WifiMoveAsync` is marked not working. Many methods contain commented-out experiments; treat them as notes on what was tried against the device, not dead code to clean up blindly.
