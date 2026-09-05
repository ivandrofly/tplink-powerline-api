using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TpLink.Api.Helpers;
using TpLink.Api.Models;
using TpLink.Api.PropertyNamingPolicy;

namespace TpLink.Api
{
    public class TpLinkClient : ITpLinkClient
    {
        /// <summary>UDP port the adapter listens on for the discovery broadcast.</summary>
        public const int DiscoveryPort = 1040;

        /// <summary>Local UDP port the discovery reply is received on (open it in the firewall).</summary>
        public const int DiscoveryListenPort = 61000;

        /// <summary>How long <see cref="DiscoveryAsync()"/> waits for an adapter to answer.</summary>
        public static readonly TimeSpan DefaultDiscoveryTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Serializer options shared by every call, for reading responses and for turning models into form fields:
        /// case-insensitive matching, lower-case property names (the device's field names are lower-case) and nulls
        /// skipped so they are never emitted as empty form fields.
        /// </summary>
        internal static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = new TpLinkPropertyNamingPolicy(),
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly RestClient _apiConnection;

        public EndpointAuth EndpointAuth { get; }

        public TpLinkClient() : this("admin", "admin", "http://192.168.1.1")
        {
        }

        public TpLinkClient(string login, string password, string endpoint)
            : this(new EndpointAuth(login, password, endpoint))
        {
        }

        public TpLinkClient(EndpointAuth apiConnection)
        {
            EndpointAuth = apiConnection ?? throw new ArgumentNullException(nameof(apiConnection));

            _apiConnection = new RestClient(apiConnection.Endpoint)
            {
                // NOTE: Whne the version of the user agent change, this may need to be changed aswell
                // the entire request may be okay, but when the user agent's version changed, this may need to be updated aswell
                // Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.163 Safari/537.36
                // important: setting rule for user-agent in fiddler will override this, which can cause several complication

                // NOTE: NOT SUPPORTED ANYMORE!
                // UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.163 Safari/537.36", // a must!
                // Timeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds,
            };

            _apiConnection.AddDefaultHeader("Cookie", $"Authorization={StringUtils.GetAuthorization(apiConnection.Login, apiConnection.Passoword)}");
            _apiConnection.AddDefaultHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            _apiConnection.AddDefaultHeader("Accept", "application/json, text/javascript, */*; q=0.01");
            _apiConnection.AddDefaultHeader("Accept-Language", "en-US,en;q=0.9,pt-PT;q=0.8,pt;q=0.7");
            _apiConnection.AddDefaultHeader("X-Requested-With", "XMLHttpRequest");
            _apiConnection.AddDefaultHeader("Accept-Encoding", "gzip,deflate");
            _apiConnection.AddDefaultHeader("Referer", apiConnection.Endpoint);
            _apiConnection.AddDefaultHeader("Origin", apiConnection.Endpoint);
            _apiConnection.AddDefaultHeader("Connection", "keep-alive");
            _apiConnection.AddDefaultHeader("DNT", "1");

            // Ignore for now! tplink server returns wrong content-type
            //restClient.UseSystemTextJson(_option);
        }

        /// <summary>
        /// Send a request and deserialize its JSON body. The adapter labels its JSON as <c>text/html</c>, which makes
        /// RestSharp's typed <c>ExecuteAsync&lt;T&gt;</c> pick the wrong serializer, so deserialization is done here.
        /// </summary>
        /// <exception cref="TpLinkException">
        /// The request never completed (connection refused, timed out, aborted) or the device answered with a
        /// non-success HTTP status or an empty body. RestSharp does not throw in those cases: it sets
        /// <c>ResponseStatus</c>/<c>ErrorException</c> and leaves <c>Content</c> null, which used to surface as an
        /// <see cref="ArgumentNullException"/> from the deserializer.
        /// </exception>
        /// <exception cref="JsonException">The body is not the expected JSON (for example firmware that encrypts the web-admin traffic).</exception>
        private async Task<T> SendAsync<T>(RestRequest request, CancellationToken cancellationToken = default) where T : class
        {
            var response = await _apiConnection.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.ResponseStatus != ResponseStatus.Completed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var detail = response.ErrorMessage ?? response.ErrorException?.Message ?? "no details";
                throw new TpLinkException($"Request to '{request.Resource}' did not complete ({response.ResponseStatus}): {detail}", response.ErrorException)
                {
                    StatusCode = response.StatusCode,
                    TimedOut = response.ResponseStatus == ResponseStatus.TimedOut,
                };
            }

            if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(response.Content))
            {
                var body = string.IsNullOrEmpty(response.Content) ? "an empty body" : "a body";
                throw new TpLinkException($"Request to '{request.Resource}' returned HTTP {(int)response.StatusCode} {response.StatusDescription} with {body}")
                {
                    StatusCode = response.StatusCode,
                };
            }

            return JsonSerializer.Deserialize<T>(response.Content, JsonOptions)
                   ?? throw new TpLinkException($"Request to '{request.Resource}' returned a JSON null body")
                   {
                       StatusCode = response.StatusCode,
                   };
        }

        /// <summary>
        /// Emit every non-null property of <paramref name="model"/> as a form field. The model is serialized with
        /// <see cref="JsonOptions"/> and flattened, so <c>JsonPropertyName</c> attributes, the lower-case naming policy
        /// and the converters in <c>TpLink.Api.Converters</c> all apply, exactly as they do when the model is read.
        /// </summary>
        /// <remarks>
        /// Replaces a reflection loop that cast every property to <c>string</c> (it would have thrown for the first
        /// bool or converter-backed property) and emitted null properties as empty fields.
        /// </remarks>
        internal static void AddFormFields<T>(RestRequest request, T model)
        {
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(model, JsonOptions));
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Null)
                {
                    continue;
                }

                var value = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : property.Value.GetRawText();

                request.AddParameter(property.Name, value, ParameterType.GetOrPost);
            }
        }

        /// <summary>
        /// Get System logs
        /// </summary>
        public Task<TpLinkResponse<List<SystemLog>>> GetSystemLogsAsync()
        {
            var req = new RestRequest("admin/syslog", Method.Post);
            req.AddParameter("form", "log", ParameterType.QueryString);
            req.AddParameter("operation", "load", ParameterType.GetOrPost);

            return SendAsync<TpLinkResponse<List<SystemLog>>>(req);
        }

        /// <summary>
        /// Get wireless clients connected to the powerline
        /// </summary>
        public Task<TpLinkClientData> GetClientsAsync()
        {
            var req = new RestRequest("admin/wireless", Method.Post);
            req.AddParameter("form", "statistics", ParameterType.GetOrPost);
            req.AddParameter("operation", "load", ParameterType.GetOrPost);

            //restClient.AddHandler("text/html", () => new JsonSerializer(_option));
            //restClient.RemoveHandler("text/html"); // exception
            // IMPORTANT: TP-LINK SERVER DOESN'T RETURN THE CORRECT CONTENT TYPE WHICH
            // MAKE THE JSONSERIALIZER TO USE THE XML BY DEFAULT
            return SendAsync<TpLinkClientData>(req);
        }

        /// <summary>
        /// Get powerline status, including transfer and received data rate
        /// </summary>
        public Task<TpLinkResponse<IList<Device>>> GetPowerlineDevicesStatusAsync()
        {
            var req = new RestRequest("admin/powerline", Method.Post);
            req.AddQueryParameter("form", "plc_device");
            req.AddParameter("operation", "load", ParameterType.GetOrPost);

            return SendAsync<TpLinkResponse<IList<Device>>>(req);
        }

        /// <summary>
        /// Get number of clients currently connected. Returns 0 when the device rejects the request
        /// (check <see cref="GetClientsAsync"/> and its <c>Success</c> flag for the reason).
        /// </summary>
        public async Task<int> GetCountConnectedClientsAsync()
        {
            var clients = await GetClientsAsync().ConfigureAwait(false);
            return clients.Data?.Count ?? 0;
        }

        /// <summary>
        /// Read the current 2.4 GHz wireless settings (admin/wireless?form=wireless_2g, operation=read).
        /// </summary>
        public Task<TpLinkResponse<WirelessModel>> GetWirelessBand2GAsync() => GetWirelessBandAsync("wireless_2g");

        /// <summary>
        /// Read the current 5 GHz wireless settings (admin/wireless?form=wireless_5g, operation=read).
        /// </summary>
        public Task<TpLinkResponse<WirelessModel>> GetWirelessBand5GAsync() => GetWirelessBandAsync("wireless_5g");

        private Task<TpLinkResponse<WirelessModel>> GetWirelessBandAsync(string form)
        {
            var req = new RestRequest("admin/wireless", Method.Post);
            req.AddQueryParameter("form", form);
            req.AddParameter("operation", "read", ParameterType.GetOrPost);

            // note: earlier experiments that poked at "data.enable" directly (Newtonsoft JObject.SelectToken,
            // JsonDocument.GetProperty, Deserialize<dynamic>) were dropped in favour of deserializing the whole model.
            return SendAsync<TpLinkResponse<WirelessModel>>(req);
        }

        public Task<TpLinkResponse<WirelessModel>> ChangeWireless2GStatusAsync(bool enabled) => ChangeWirelessStatusAsync("wireless_2g", enabled);

        public Task<TpLinkResponse<WirelessModel>> ChangeWireless5GStatusAsync(bool enabled) => ChangeWirelessStatusAsync("wireless_5g", enabled);

        /// <summary>
        /// Turn a radio on or off without touching any other setting: read the band's current settings,
        /// flip <c>enable</c>, and post the whole model back (operation=write), exactly as the web UI does.
        /// </summary>
        /// <remarks>
        /// The previous 5 GHz implementation only read ssid/psk_key/encryption (from admin/wlan_status) and
        /// hard-coded hidden=off, psk_version=auto, psk_cipher=auto, hwmode=a, htmode=80, channel=auto and
        /// txpower=low, so every toggle silently reset a hidden SSID, a fixed channel or the transmit power.
        /// </remarks>
        private async Task<TpLinkResponse<WirelessModel>> ChangeWirelessStatusAsync(string form, bool enabled)
        {
            var current = await GetWirelessBandAsync(form).ConfigureAwait(false);
            if (current.Data == null)
            {
                // the device refused the read (typically because the web manager is open in a browser);
                // hand that envelope back so the caller sees Success == false instead of a NullReferenceException
                return current;
            }

            current.Data.Enable = enabled ? "on" : "off";

            var req = new RestRequest("admin/wireless", Method.Post);
            req.AddQueryParameter("form", form);
            req.AddParameter("operation", "write", ParameterType.GetOrPost);
            // note: req.AddObject(model) throws for this shape, hence the explicit flattening
            AddFormFields(req, current.Data);

            return await SendAsync<TpLinkResponse<WirelessModel>>(req).ConfigureAwait(false);
        }

        public Task<TpLinkResponse<WifiMove>> WifiMoveAsync(bool enabled)
        {
            var req = new RestRequest("/admin/wifiMove.json", Method.Post);
            req.AddParameter("operation", "write", ParameterType.GetOrPost);
            req.AddParameter("enable", enabled ? 1 : 0, ParameterType.GetOrPost);

            // TODO: NOT WORKING, BUT THE REQUEST LOOKS THE SAME AS FROM CHROME BROWSER!
            return SendAsync<TpLinkResponse<WifiMove>>(req);
        }

        public async Task<TpLinkResponse<bool>> RebootAsync()
        {
            var req = new RestRequest("/admin/reboot.json", Method.Post);
            req.AddParameter("operation", "write", ParameterType.GetOrPost);

            // the device drops the connection while it reboots, so there is often no usable body:
            // this call is fire-and-forget and deliberately bypasses SendAsync
            _ = await _apiConnection.ExecuteAsync(req).ConfigureAwait(false);
            return new TpLinkResponse<bool> { Success = true, Data = true };
        }

        public Task<TpLinkResponse<Guest2G>> GetGuest2GhzAsync()
        {
            var req = new RestRequest("/admin/guest?form=guest_2g", Method.Post)
            {
                Timeout = TimeSpan.FromSeconds(3)
            };
            req.AddQueryParameter("form", "guest_2g");
            req.AddParameter("operation", "read");

            // a timeout (guest network disabled in the router?) surfaces as TpLinkException.TimedOut
            return SendAsync<TpLinkResponse<Guest2G>>(req);
        }

        public Task<TpLinkResponse<Guest5G>> GetGuest5GhzAsync()
        {
            var req = new RestRequest("/admin/guest?form=guest_5g", Method.Post);
            req.AddQueryParameter("form", "guest_5g");
            req.AddParameter("operation", "read");

            return SendAsync<TpLinkResponse<Guest5G>>(req);
        }

        /// <summary>
        /// Find out which ip address is assigned to the powerline, waiting up to <see cref="DefaultDiscoveryTimeout"/>.
        /// </summary>
        /// <returns>The ip address of the powerline</returns>
        /// <exception cref="TimeoutException">No adapter answered in time.</exception>
        public static Task<string> DiscoveryAsync() => DiscoveryAsync(DefaultDiscoveryTimeout);

        /// <summary>
        /// Find out which ip address is assigned to the powerline by broadcasting on UDP <see cref="DiscoveryPort"/>
        /// and listening on <see cref="DiscoveryListenPort"/>. The first adapter that answers wins.
        /// </summary>
        /// <param name="timeout">How long to wait for an answer.</param>
        /// <param name="cancellationToken">Cancels the wait early.</param>
        /// <returns>The ip address of the powerline</returns>
        /// <exception cref="TimeoutException">No adapter answered within <paramref name="timeout"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
        public static async Task<string> DiscoveryAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            // note here 192.168.1.86 was the ip that powerline was using - this is dynamic can change
            // wireshark filter: (ip.dst == 192.168.1.108 && ip.src == 192.168.1.86 ) || (ip.dst == 255.255.255.255)
            // original discover message captured in wireshark (bytes); the plain "Where are you!" text below works too:
            // 02 03 01 00 00 00 00 00 e8 03 12 00 75 7c be 42 dc 81 21 f6 e1 5e ff c0 c4 1e 25 96
            using var uc = new UdpClient();
            // let a second instance (or a socket lingering from the previous run) bind the same port instead of throwing
            uc.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            uc.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryListenPort));
            uc.EnableBroadcast = true;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                var buffer = Encoding.UTF8.GetBytes("Where are you!");

                // note: see https://stackoverflow.com/a/40617102/2766753 for a more reliable udp implementation
                await uc.SendAsync(buffer, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort), cts.Token).ConfigureAwait(false);

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
                    $"No TP-Link powerline adapter answered the discovery broadcast (UDP {DiscoveryListenPort} -> {DiscoveryPort}) " +
                    $"within {timeout.TotalSeconds:0.#} s. Make sure no VPN is active, the adapter is on this LAN and the firewall " +
                    $"allows inbound UDP on port {DiscoveryListenPort}, or pass the adapter's IP to the constructor instead.");
            }
        }

        public Task<object> AddNewUserAsync()
        {
            throw new NotImplementedException();
        }

        public Task<TpLinkResponse<ICollection<WifiSchedule>>> AddNewWifiScheduleAsync(WifiSchedule wifiSchedule)
        {
            // validation
            if (wifiSchedule.StartTime < 0 || wifiSchedule.StartTime > 24)
            {
                throw new InvalidEnumArgumentException(nameof(WifiSchedule.StartTime));
            }

            if (wifiSchedule.EndTime < 0 || wifiSchedule.EndTime > 24)
            {
                throw new InvalidEnumArgumentException(nameof(WifiSchedule.EndTime));
            }

            if (wifiSchedule.StartTime >= wifiSchedule.EndTime)
            {
                throw new InvalidEnumArgumentException(nameof(WifiSchedule.StartTime));
            }

            // JsonConverterFactory
            // https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to#support-dictionary-with-non-string-key
            // https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to#registration-sample---converters-collection

            var req = new RestRequest("/admin/wlanTimeControl", Method.Post);
            req.AddParameter("operation", "insert");
            req.AddParameter("key", "add");
            req.AddParameter("index", "0"); // i think the index should be get from sorting all the pre existing rules and insert acoording to "from" time
            req.AddParameter("old", "add");
            req.AddParameter("new", JsonSerializer.Serialize(wifiSchedule, JsonOptions));

            // note: this used to deserialize without the shared options, so "success"/"timeout" never mapped
            // onto Success/Timeout and every caller saw Success == false
            return SendAsync<TpLinkResponse<ICollection<WifiSchedule>>>(req);
        }

        public Task<MacFilterDevice> AddMacFilterAsync(MacFilterDevice macFilter)
        {
            throw new NotImplementedException();
        }

        public Task<MacFilterDevice> RemoveMacFilterAsync(MacFilterDevice macFilter)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ChangeMacFilterStateAsync(bool enabled)
        {
            throw new NotImplementedException();
        }

        public Task<TpLinkResponse<ICollection<MacFilterDevice>>> GetMacFilterGetDevicesAsync(bool enabled)
        {
            throw new NotImplementedException();
        }

        // note: copied code from my networking->UDPTesting project example
        //public static IPAddress GetIpAddress()
        //{
        //    const NetworkInterfaceType interfaceType = true
        //        ? NetworkInterfaceType.Wireless80211
        //        : NetworkInterfaceType.Ethernet; /*| NetworkInterfaceType.FastEthernetFx |
        //          NetworkInterfaceType.GigabitEthernet;*/ // "|" won't work because the type doesn't use [Flag] attribuite

        //    IPAddress found = default;

        //    // NOTE: ALWAYS SPECIFY THE INTERFACE IF THERE IS MORE THAN ON CONNECTION (LAN-WIFI)
        //    foreach (var @interface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
        //    {
        //        Console.WriteLine(@interface.Name);
        //        if (@interface.NetworkInterfaceType == interfaceType)
        //        {
        //            // could be ipv4 / ipv6
        //            var address = @interface.GetIPProperties().UnicastAddresses
        //                .First(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork).Address;
        //            // note: if you are using virtual box this could be it's address so make sure more
        //            // filter is done...
        //            Console.WriteLine(address.ToString());
        //            found = address;
        //        }
        //    }

        //    var ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[2];
        //    Console.WriteLine(ipAddress);
        //    return ipAddress;
        //}
    }
}
