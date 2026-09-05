using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
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

        /// <summary>How long a single HTTP request may take before it fails with <see cref="TpLinkException.TimedOut"/>.</summary>
        public static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The browser the web UI was captured with. The whole request mimics that browser (see the default headers
        /// in the constructor); when this version changes the device may need the other headers refreshed as well.
        /// A user-agent rewrite rule in Fiddler overrides it and causes odd failures.
        /// </summary>
        private const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.163 Safari/537.36";

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
        private bool _disposed;

        /// <summary>Where the adapter is and who we log in as. The password itself is not exposed.</summary>
        public EndpointAuth EndpointAuth { get; }

        public string Endpoint => EndpointAuth.Endpoint;

        public TpLinkClient() : this("admin", "admin", "http://192.168.1.1")
        {
        }

        public TpLinkClient(string login, string password, string endpoint)
            : this(new EndpointAuth(login, password, endpoint))
        {
        }

        public TpLinkClient(EndpointAuth apiConnection) : this(apiConnection, DefaultRequestTimeout)
        {
        }

        public TpLinkClient(EndpointAuth apiConnection, TimeSpan requestTimeout)
        {
            EndpointAuth = apiConnection ?? throw new ArgumentNullException(nameof(apiConnection));
            if (requestTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(requestTimeout), requestTimeout, "The request timeout must be positive.");
            }

            _apiConnection = new RestClient(new RestClientOptions(apiConnection.Endpoint)
            {
                // without this a hung adapter blocks for HttpClient's 100 s default
                Timeout = requestTimeout,
                UserAgent = BrowserUserAgent,
            });

            _apiConnection.AddDefaultHeader("Cookie", $"Authorization={apiConnection.BuildAuthorizationCookie()}");
            _apiConnection.AddDefaultHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            _apiConnection.AddDefaultHeader("Accept", "application/json, text/javascript, */*; q=0.01");
            _apiConnection.AddDefaultHeader("Accept-Language", "en-US,en;q=0.9,pt-PT;q=0.8,pt;q=0.7");
            _apiConnection.AddDefaultHeader("X-Requested-With", "XMLHttpRequest");
            _apiConnection.AddDefaultHeader("Accept-Encoding", "gzip,deflate");
            _apiConnection.AddDefaultHeader("Referer", apiConnection.Endpoint);
            _apiConnection.AddDefaultHeader("Origin", apiConnection.Endpoint);
            _apiConnection.AddDefaultHeader("Connection", "keep-alive");
            _apiConnection.AddDefaultHeader("DNT", "1");

            // note: RestClient.UseSystemTextJson was never an option: the adapter labels its JSON as text/html,
            // so the typed ExecuteAsync<T> picks the wrong serializer. See SendAsync.
        }

        /// <summary>
        /// Create a client from <paramref name="options"/>, discovering the adapter on the LAN when
        /// <see cref="TpLinkOptions.Endpoint"/> is empty.
        /// </summary>
        /// <exception cref="InvalidOperationException">Login or password is not configured.</exception>
        /// <exception cref="TimeoutException">Discovery was needed and no adapter answered.</exception>
        public static async Task<TpLinkClient> CreateAsync(TpLinkOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            options.Validate();

            var endpoint = options.Endpoint;
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                var ip = await DiscoveryAsync(options.DiscoveryTimeout, cancellationToken).ConfigureAwait(false);
                endpoint = $"http://{ip}";
            }

            return new TpLinkClient(new EndpointAuth(options.Login, options.Password, endpoint), options.RequestTimeout);
        }

        /// <summary>
        /// Build the request shape every endpoint uses: <c>{path}?form={form}</c> with <c>operation</c> (and any
        /// further fields) in the url-encoded body. The web UI always carries <c>form</c> in the query string.
        /// </summary>
        private static RestRequest NewFormRequest(string path, string form, string operation)
        {
            var req = new RestRequest(path, Method.Post);
            if (form != null)
            {
                req.AddQueryParameter("form", form);
            }

            req.AddParameter("operation", operation, ParameterType.GetOrPost);
            return req;
        }

        /// <summary>
        /// Send a request and deserialize its JSON body. The adapter labels its JSON as <c>text/html</c>, which makes
        /// RestSharp's typed <c>ExecuteAsync&lt;T&gt;</c> pick the wrong serializer, so deserialization is done here.
        /// </summary>
        /// <exception cref="TpLinkException">
        /// The request never completed (connection refused, timed out, aborted) or the device answered with a
        /// non-success HTTP status or an empty body. RestSharp does not throw in those cases: it sets
        /// <c>ResponseStatus</c>/<c>ErrorException</c> and leaves <c>Content</c> null. A timeout is reported as
        /// <c>ResponseStatus.TimedOut</c> with status code 0, never as HTTP 408.
        /// </exception>
        /// <exception cref="JsonException">The body is not the expected JSON (for example firmware that encrypts the web-admin traffic).</exception>
        private async Task<T> SendAsync<T>(RestRequest request, CancellationToken cancellationToken) where T : class
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

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

        /// <inheritdoc />
        public Task<TpLinkResponse<List<SystemLog>>> GetSystemLogsAsync(CancellationToken cancellationToken = default)
        {
            var req = NewFormRequest("admin/syslog", "log", "load");
            return SendAsync<TpLinkResponse<List<SystemLog>>>(req, cancellationToken);
        }

        /// <inheritdoc />
        public Task<TpLinkClientData> GetClientsAsync(CancellationToken cancellationToken = default)
        {
            // note: this used to send form=statistics in the POST body rather than the query string; the device
            // accepted both, and the query string is what the web UI sends
            var req = NewFormRequest("admin/wireless", "statistics", "load");
            return SendAsync<TpLinkClientData>(req, cancellationToken);
        }

        /// <inheritdoc />
        public Task<TpLinkResponse<IList<Device>>> GetPowerlineDevicesStatusAsync(CancellationToken cancellationToken = default)
        {
            var req = NewFormRequest("admin/powerline", "plc_device", "load");
            return SendAsync<TpLinkResponse<IList<Device>>>(req, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> GetConnectedClientCountAsync(CancellationToken cancellationToken = default)
        {
            var clients = await GetClientsAsync(cancellationToken).ConfigureAwait(false);
            return clients.Data?.Count ?? 0;
        }

        /// <inheritdoc />
        public Task<TpLinkResponse<WirelessModel>> GetWirelessBand2GAsync(CancellationToken cancellationToken = default) =>
            GetWirelessBandAsync("wireless_2g", cancellationToken);

        /// <inheritdoc />
        public Task<TpLinkResponse<WirelessModel>> GetWirelessBand5GAsync(CancellationToken cancellationToken = default) =>
            GetWirelessBandAsync("wireless_5g", cancellationToken);

        private Task<TpLinkResponse<WirelessModel>> GetWirelessBandAsync(string form, CancellationToken cancellationToken)
        {
            var req = NewFormRequest("admin/wireless", form, "read");

            // note: earlier experiments that poked at "data.enable" directly (Newtonsoft JObject.SelectToken,
            // JsonDocument.GetProperty, Deserialize<dynamic>) were dropped in favour of deserializing the whole model.
            return SendAsync<TpLinkResponse<WirelessModel>>(req, cancellationToken);
        }

        /// <inheritdoc />
        public Task<TpLinkResponse<WirelessModel>> ChangeWireless2GStatusAsync(bool enabled, CancellationToken cancellationToken = default) =>
            ChangeWirelessStatusAsync("wireless_2g", enabled, cancellationToken);

        /// <inheritdoc />
        public Task<TpLinkResponse<WirelessModel>> ChangeWireless5GStatusAsync(bool enabled, CancellationToken cancellationToken = default) =>
            ChangeWirelessStatusAsync("wireless_5g", enabled, cancellationToken);

        /// <summary>
        /// Turn a radio on or off without touching any other setting: read the band's current settings,
        /// flip <c>enable</c>, and post the whole model back (operation=write), exactly as the web UI does.
        /// </summary>
        /// <remarks>
        /// The previous 5 GHz implementation only read ssid/psk_key/encryption (from admin/wlan_status) and
        /// hard-coded hidden=off, psk_version=auto, psk_cipher=auto, hwmode=a, htmode=80, channel=auto and
        /// txpower=low, so every toggle silently reset a hidden SSID, a fixed channel or the transmit power.
        /// </remarks>
        private async Task<TpLinkResponse<WirelessModel>> ChangeWirelessStatusAsync(string form, bool enabled, CancellationToken cancellationToken)
        {
            var current = await GetWirelessBandAsync(form, cancellationToken).ConfigureAwait(false);
            if (current.Data == null)
            {
                // the device refused the read (typically because the web manager is open in a browser);
                // hand that envelope back so the caller sees Success == false
                return current;
            }

            current.Data.Enable = enabled ? "on" : "off";

            var req = NewFormRequest("admin/wireless", form, "write");
            // note: req.AddObject(model) throws for this shape, hence the explicit flattening
            AddFormFields(req, current.Data);

            return await SendAsync<TpLinkResponse<WirelessModel>>(req, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<TpLinkResponse<WifiMove>> SetWifiMoveAsync(bool enabled, CancellationToken cancellationToken = default)
        {
            var req = NewFormRequest("admin/wifiMove.json", form: null, "write");
            req.AddParameter("enable", enabled ? 1 : 0, ParameterType.GetOrPost);

            // TODO: NOT WORKING, BUT THE REQUEST LOOKS THE SAME AS FROM CHROME BROWSER!
            return SendAsync<TpLinkResponse<WifiMove>>(req, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TpLinkResponse<bool>> RebootAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var req = NewFormRequest("admin/reboot.json", form: null, "write");

            // the device drops the connection while it reboots, so there is often no usable body:
            // this call is fire-and-forget and deliberately bypasses SendAsync
            _ = await _apiConnection.ExecuteAsync(req, cancellationToken).ConfigureAwait(false);
            return new TpLinkResponse<bool> { Success = true, Data = true };
        }

        /// <inheritdoc />
        public Task<TpLinkResponse<Guest2G>> GetGuest2GAsync(CancellationToken cancellationToken = default)
        {
            // a timeout here (guest network disabled in the router?) surfaces as TpLinkException.TimedOut
            var req = NewFormRequest("admin/guest", "guest_2g", "read");
            return SendAsync<TpLinkResponse<Guest2G>>(req, cancellationToken);
        }

        /// <inheritdoc />
        public Task<TpLinkResponse<Guest5G>> GetGuest5GAsync(CancellationToken cancellationToken = default)
        {
            var req = NewFormRequest("admin/guest", "guest_5g", "read");
            return SendAsync<TpLinkResponse<Guest5G>>(req, cancellationToken);
        }

        /// <inheritdoc />
        public Task<TpLinkResponse<ICollection<WifiSchedule>>> AddNewWifiScheduleAsync(WifiSchedule wifiSchedule, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(wifiSchedule);
            if (wifiSchedule.StartTime is < 0 or > 24)
            {
                throw new ArgumentOutOfRangeException(nameof(wifiSchedule), wifiSchedule.StartTime, "StartTime must be an hour between 0 and 24.");
            }

            if (wifiSchedule.EndTime is < 0 or > 24)
            {
                throw new ArgumentOutOfRangeException(nameof(wifiSchedule), wifiSchedule.EndTime, "EndTime must be an hour between 0 and 24.");
            }

            if (wifiSchedule.StartTime >= wifiSchedule.EndTime)
            {
                throw new ArgumentException("StartTime must be earlier than EndTime.", nameof(wifiSchedule));
            }

            var req = NewFormRequest("admin/wlanTimeControl", form: null, "insert");
            req.AddParameter("key", "add", ParameterType.GetOrPost);
            req.AddParameter("index", "0", ParameterType.GetOrPost); // i think the index should be get from sorting all the pre existing rules and insert according to "from" time
            req.AddParameter("old", "add", ParameterType.GetOrPost);
            req.AddParameter("new", JsonSerializer.Serialize(wifiSchedule, JsonOptions), ParameterType.GetOrPost);

            return SendAsync<TpLinkResponse<ICollection<WifiSchedule>>>(req, cancellationToken);
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

        /// <summary>Releases the underlying HTTP connection. Further calls throw <see cref="ObjectDisposedException"/>.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _apiConnection.Dispose();
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
