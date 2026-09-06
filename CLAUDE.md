# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET 8 client library (`TpLink.Api`) that drives the web-admin interface of a TP-Link TL-WPA8630P powerline adapter by replaying the same HTTP form posts the browser UI makes. Tested only on that model. Two small host apps (`Client.Console`, `TpLink.Service`) exercise it, and `TpLink.UnitTest` holds xUnit tests.

## Commands

The solution is `tplink-powerline.slnx` (the XML solution format), so the .NET SDK must be 9.0.200 or newer to build it. Bare `dotnet build` and `dotnet test` from the repo root work.

```
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~WifiScheduleTest"
dotnet run --project Client.Console -- clients      # on | off | reboot | clients | batch
dotnet run --project TpLinkDataRate/TpLink.Service.csproj
```

`global.json` pins the 9.0 feature band (`rollForward: latestFeature`, no prereleases), so local builds and CI use the same SDK line. CI (`.github/workflows/dotnet.yml`) runs restore, a Release build and the tests on the .NET 9.0.x SDK and uploads trx results plus coverage; projects still target `net8.0`. `Directory.Build.props` pins `net8.0` and sets `TreatWarningsAsErrors`, so a new compiler warning fails the build. `Directory.Packages.props` does central package version management, so add new packages there and reference them without a `Version` in the csproj.

Running either host app needs a live adapter on the LAN plus credentials in a `TpLinkOptions`: the service binds the `TpLink` configuration section (appsettings, user-secrets, `TpLink__*` variables) and both apps fall back to the `tplink_powerline_login`, `tplink_powerline_pwd` and optional `tplink_powerline_endpoint` process environment variables via `TpLinkOptions.ApplyEnvironmentFallback` (`setx` or `$env:` on Windows, `export` elsewhere; never `EnvironmentVariableTarget.User`, which returns null on Linux/macOS). `TpLinkClient.CreateAsync(options)` locates the adapter with a UDP broadcast (`TpLinkClient.DiscoveryAsync`) when no endpoint is configured; discovery fails through a VPN or when several adapters are up. Requests also fail while the adapter's web manager is open in a browser, because the device allows one session.

## Architecture

**Auth is a cookie, not a header.** `StringUtils.GetAuthorization` builds `Basic {login}:{md5(password)}`, URL-escapes it, and `TpLinkClient` sets it as a default `Cookie: Authorization=...` header on the shared `RestClient`. The unit test pins this exact string, so changing hashing or escaping breaks it. `EndpointAuth` is immutable and keeps the password private; only `BuildAuthorizationCookie()` (internal) derives from it, so nothing else in the library or the hosts should ever hold or log the plaintext password.

**Client lifetime.** `RestClient` is built with `RestClientOptions` (10 s request timeout, browser user agent) and owns an `HttpClient`, so `TpLinkClient` is `IDisposable` and `ITpLinkClient` extends it. Prefer `TpLinkClient.CreateAsync(TpLinkOptions)` in hosts; it validates the options and runs discovery when no endpoint is set.

**Every call is a form post.** Endpoints look like `admin/wireless?form=wireless_2g` with `operation=load|read|write|insert` and other fields as `application/x-www-form-urlencoded` parameters via `RestRequest.AddParameter(..., ParameterType.GetOrPost)`. The private `NewFormRequest(path, form, operation)` builds that shape (`form` always in the query string, `operation` in the body); use it for new endpoints. Nothing is sent as a JSON body. When writing settings back (see the private `ChangeWirelessStatusAsync`, shared by the 2.4 GHz and 5 GHz toggles), the client reads the current model, mutates it, then calls `TpLinkClient.AddFormFields`, which serializes the model with the shared `JsonOptions` and flattens the result into one form field per non-null property. `JsonPropertyName` attributes, the lower-casing `TpLinkPropertyNamingPolicy` and the converters therefore shape both directions. Never post hard-coded values for fields the device already reports; that silently resets user settings.

**Deserialization is manual.** The adapter returns JSON with `Content-Type: text/html`, which makes RestSharp pick the wrong serializer. Every method therefore goes through the private `SendAsync<T>`, which calls `_apiConnection.ExecuteAsync(req)` and runs `System.Text.Json.JsonSerializer.Deserialize<T>(response.Content, JsonOptions)` itself. Keep that pattern rather than switching to `ExecuteAsync<T>`. `RebootAsync` is the one method that bypasses it, because the device drops the connection while rebooting.

**Failure semantics.** RestSharp never throws for transport problems; it leaves `Content` null. `SendAsync<T>` turns that (and any non-2xx or empty response) into a `TpLinkException` with `StatusCode` and `TimedOut`. A request the device accepted but rejected comes back as an envelope with `Success == false` and `Data == null`, so callers must null-check `Data`. Invalid JSON (encrypted firmware) propagates as `JsonException`.

**Response envelope.** Responses deserialize into `TpLinkResponse<TData>` with `Success`, `Timeout`, and `Data`. `TpLinkClientData` extends it with `max_rules` for the wireless client list. Device fields arrive as strings ("on"/"off", "1"/"0", numbers as text), so models keep `string` properties or use the converters in `TpLink.Api/Converters` (`StringBoolConverter` for on/off, `BoolToBitConvert` for 1/0, `IntToString`, `DaysEnumToCustomString`). The converters switch on `reader.TokenType` (shared logic in `JsonTokenParsing`) and tolerate JSON booleans, numbers and null; keep that when adding one, and add a theory case per token kind in `ConvertersTest`. `WifiSchedule` shows the full pattern: a `[Flags] Days` enum serialized as its byte value plus one derived `week_*` bit property per day.

**Host apps.** `Client.Console` (namespace `TpLink.Cli`, so `Console` is not shadowed) uses a command pattern (`ICommand.Execute(ITpLinkClient)` with `Invoker` wiring `TurnOnSignal`, `TurnOffSignal`, `RebootCommand`, `DisplayConnectedCommand`); `Program.cs` is top-level statements that dispatch on the single command-line argument and print usage otherwise. `TpLinkDataRate` is a generic-host `BackgroundService` (`Worker`) that binds `TpLinkOptions` and `WorkerOptions`, creates the client with `TpLinkClient.CreateAsync` inside `ExecuteAsync` (so discovery never blocks host start-up), and logs powerline link rates every `Worker:PollInterval`.

## State of the code

`SetWifiMoveAsync` is marked not working. User management and MAC filtering are not implemented and no longer have placeholder members. Some methods contain commented-out experiments; treat them as notes on what was tried against the device, not dead code to clean up blindly.
