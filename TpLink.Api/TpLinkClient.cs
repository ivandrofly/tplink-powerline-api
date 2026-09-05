using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TpLink.Api.Helpers;
using TpLink.Api.Models;

namespace TpLink.Api
{
    public class TpLinkClient : ITpLinkClient
    {
        private readonly RestClient _apiConnection; // name "connection" doesn't handle the validation
        private readonly JsonSerializerOptions jsonOption;

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

            // default serializer options
            jsonOption = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
                // note: wont work when sending the request, since the request ain't sent as json! :(
                //PropertyNamingPolicy = new TpLinkPropertyNamingPolicy(),
            };
            // Ignore for now! tplink server returns wrong content-type
            //restClient.UseSystemTextJson(_option);
        }

        /// <summary>
        /// Get System logs
        /// </summary>
        public async Task<TpLinkResponse<List<SystemLog>>> GetSystemLogsAsync()
        {
            var req = new RestRequest("admin/syslog", Method.Post);
            req.AddParameter("form", "log", ParameterType.QueryString);
            req.AddParameter("operation", "load", ParameterType.GetOrPost);

            // note: since the response comes wiht "content-type: text/html" it json parser will fail to parse it
            var response = await _apiConnection.ExecuteAsync(req).ConfigureAwait(false);

            if (!response.IsSuccessful)
            {
                return default;
            }

            return JsonSerializer.Deserialize<TpLinkResponse<List<SystemLog>>>(response.Content, jsonOption) ?? new TpLinkResponse<List<SystemLog>>();
        }

        /// <summary>
        /// Get wireless clients connected to the powerline
        /// </summary>
        //public async Task<TpLinkClientData> GetClientsAsync()
        public async Task<TpLinkClientData> GetClientsAsync()
        {
            var req = new RestRequest("admin/wireless", Method.Post);
            req.AddParameter("form", "statistics", ParameterType.GetOrPost);
            req.AddParameter("operation", "load", ParameterType.GetOrPost);

            //restClient.AddHandler("text/html", () => new JsonSerializer(_option));
            //restClient.RemoveHandler("text/html"); // exception
            // use system json serializer
            // IMPORTANT: TP-LINK SERVER DOESN'T RETURN THE CORRECT CONTENT TYPE WHICH
            // MAKE THE JSONSERIALIZER TO USE THE XML BY DEFAULT
            var response = await _apiConnection.ExecuteAsync(req).ConfigureAwait(false);

            // faulty response
            //var doc = JsonDocument.Parse(response.Content, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            //if (doc.RootElement.GetProperty("success").GetBoolean() == true)
            //{
            //    Console.WriteLine("failed");
            //}

            return JsonSerializer.Deserialize<TpLinkClientData>(response.Content, jsonOption);
        }

        /// <summary>
        /// Get powerline status, including transfer and received data rate
        /// </summary>
        public async Task<TpLinkResponse<IList<Device>>> GetPowerlineDevicesStatusAsync()
        {
            var req = new RestRequest("admin/powerline", Method.Post);
            req.AddQueryParameter("form", "plc_device");
            req.AddParameter("operation", "load", ParameterType.GetOrPost);

            // IMPORTANT: TP-LINK SERVER RETURNS WRONG CONTENT TYPE (TEXT/HTML) WHICH INVOKES XML SERIALIZER BY DEFAULT
            //var response = await restClient.ExecuteAsync<TpLinkData<SystemLog>>(powerLineStatusRequest);

            var response = await _apiConnection.ExecuteAsync(req).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return default;
            }

            return JsonSerializer.Deserialize<TpLinkResponse<IList<Device>>>(response.Content, jsonOption);
        }

        /// <summary>
        /// Get number of clients currently connected
        /// </summary>
        public async Task<int> GetCountConnectedClientsAsync()
        {
            var clients = await GetClientsAsync();
            return clients.Data.Count;
        }

        /// <summary>
        /// Read the current 2.4 GHz wireless settings (admin/wireless?form=wireless_2g, operation=read).
        /// </summary>
        public Task<TpLinkResponse<WirelessModel>> GetWirelessBand2GAsync() => GetWirelessBandAsync("wireless_2g");

        /// <summary>
        /// Read the current 5 GHz wireless settings (admin/wireless?form=wireless_5g, operation=read).
        /// </summary>
        public Task<TpLinkResponse<WirelessModel>> GetWirelessBand5GAsync() => GetWirelessBandAsync("wireless_5g");

        private async Task<TpLinkResponse<WirelessModel>> GetWirelessBandAsync(string form)
        {
            var req = new RestRequest("admin/wireless", Method.Post);
            req.AddQueryParameter("form", form);
            req.AddParameter("operation", "read", ParameterType.GetOrPost);
            var res = await _apiConnection.ExecuteAsync(req).ConfigureAwait(false);

            // note: earlier experiments that poked at "data.enable" directly (Newtonsoft JObject.SelectToken,
            // JsonDocument.GetProperty, Deserialize<dynamic>) were dropped in favour of deserializing the whole model.
            return JsonSerializer.Deserialize<TpLinkResponse<WirelessModel>>(res.Content, jsonOption);
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
            if (current?.Data == null)
            {
                // the device refused the read (typically because the web manager is open in a browser);
                // hand that envelope back so the caller sees Success == false instead of a NullReferenceException
                return current;
            }

            current.Data.Enable = enabled ? "on" : "off";

            var req = new RestRequest("admin/wireless", Method.Post);
            req.AddQueryParameter("form", form);
            req.AddParameter("operation", "write", ParameterType.GetOrPost);

            // emit every property the device sent us as a form field, named after its JsonPropertyName
            // (or the lower-cased property name). Properties the device did not send are skipped, so the
            // 5 GHz write does not carry the 2.4 GHz-only "wireless_2g_disabled*" fields.
            // note: req.AddObject(model) throws for this shape, hence the explicit loop.
            foreach (var prop in typeof(WirelessModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetValue(current.Data) is not string value)
                {
                    continue;
                }

                var jsonProp = prop.GetCustomAttribute<JsonPropertyNameAttribute>(true);
                req.AddParameter(jsonProp?.Name ?? prop.Name.ToLowerInvariant(), value, ParameterType.GetOrPost);
            }

            var res = await _apiConnection.ExecuteAsync(req).ConfigureAwait(false);
            return JsonSerializer.Deserialize<TpLinkResponse<WirelessModel>>(res.Content, jsonOption);
        }

        public async Task<TpLinkResponse<WifiMove>> WifiMoveAsync(bool enabled)
        {
            var req = new RestRequest("/admin/wifiMove.json", Method.Post);
            req.AddParameter("operation", "write", ParameterType.GetOrPost);
            req.AddParameter("enable", enabled ? 1 : 0, ParameterType.GetOrPost);

            // TODO: NOT WORKING, BUT THE REQUEST LOOKS THE SAME AS FROM CHROME BROWSER!
            var res = await _apiConnection.ExecuteAsync(req).ConfigureAwait(false);
            return JsonSerializer.Deserialize<TpLinkResponse<WifiMove>>(res.Content, jsonOption);
        }

        public async Task<TpLinkResponse<bool>> RebootAsync()
        {
            var req = new RestRequest("/admin/reboot.json", Method.Post);
            req.AddParameter("operation", "write", ParameterType.GetOrPost);

            _ = await _apiConnection.ExecuteAsync(req).ConfigureAwait(false);
            return await Task.FromResult(new TpLinkResponse<bool> { Data = true });
        }

        public async Task<TpLinkResponse<Guest2G>> GetGuest2GhzAsync()
        {
            var req = new RestRequest("/admin/guest?form=guest_2g", Method.Post)
            {
                Timeout = TimeSpan.FromSeconds(3)
            };
            req.AddQueryParameter("form", "guest_2g");
            req.AddParameter("operation", "read");
            var res = await _apiConnection.ExecuteAsync(req).ConfigureAwait(false);

            if (res.StatusCode == HttpStatusCode.RequestTimeout)
            {
                // isable in router
            }

            return JsonSerializer.Deserialize<TpLinkResponse<Guest2G>>(res.Content, jsonOption);
        }

        public async Task<TpLinkResponse<Guest5G>> GetGuest5GhzAsync()
        {
            var req = new RestRequest("/admin/guest?form=guest_5g", Method.Post);
            req.AddQueryParameter("form", "guest_5g");
            req.AddParameter("operation", "read");
            var res = await _apiConnection.ExecuteAsync(req).ConfigureAwait(false);

            var jdoc = JsonDocument.Parse(res.Content, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            return JsonSerializer.Deserialize<TpLinkResponse<Guest5G>>(res.Content, jsonOption);
        }

        /// <summary>
        /// Find out which ip address is signed to the powerline.
        /// </summary>
        /// <returns>The ip address of the powerline</returns>
        public static async Task<string> DiscoveryAsync()
        {
            // note here 192.168.1.86 was the ip that powerline was using - this is dynamic can change
            // wireshark filter: (ip.dst == 192.168.1.108 && ip.src == 192.168.1.86 ) || (ip.dst == 255.255.255.255) 
            using var uc = new UdpClient(new IPEndPoint(IPAddress.Any, 61000))
            {
                EnableBroadcast = true,
            };

            //uc.Client.Bind(new IPEndPoint(IPAddress.Any, 61000));

            // original data capture in wireshark (discover message in bytes)
            var data = new byte[]
            {
                0x02, 0x03, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
                0xe8, 0x03, 0x12, 0x00, 0x75, 0x7c, 0xbe, 0x42, 0xdc, 0x81,
                0x21, 0xf6, 0xe1, 0x5e, 0xff, 0xc0, 0xc4, 0x1e, 0x25, 0x96
            };

            var buffer = Encoding.UTF8.GetBytes("Where are you!");

            // note: i think this is not really safe, because if powerline was fast enough the receive won't be able to capture
            // see: https://stackoverflow.com/a/40617102/2766753 for more relaiable udp implementation
            var count = await uc.SendAsync(buffer, buffer.Length, IPAddress.Broadcast.ToString(), 1040).ConfigureAwait(false);
            UdpReceiveResult udpResponse = await uc.ReceiveAsync().ConfigureAwait(false);

            // get the discovered ip addres of the powerline
            return udpResponse.RemoteEndPoint.Address.ToString();
        }

        public Task<object> AddNewUserAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<TpLinkResponse<ICollection<WifiSchedule>>> AddNewWifiScheduleAsync(WifiSchedule wifiSchedule)
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

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                IgnoreNullValues = true,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                //PropertyNamingPolicy = new PropertyNamingPolicy.TpLinkPropertyNamingPolicy
            };

            // JsonConverterFactory
            // https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to#support-dictionary-with-non-string-key

            // https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to#registration-sample---converters-collection
            // jsonOptions.Converters.Add(new DateTimeOffsetConverter());

            // displaying registered converters
            // foreach (JsonConverter jc in jsonOptions.Converters)
            // {
            //     Console.WriteLine(jc.ToString());
            // }

            var req = new RestRequest("/admin/wlanTimeControl", Method.Post);
            req.AddParameter("operation", "insert");
            req.AddParameter("key", "add");
            req.AddParameter("index", "0"); // i think the index should be get from sorting all the pre existing rules and insert acoording to "from" time
            req.AddParameter("old", "add");
            req.AddParameter("new", JsonSerializer.Serialize(wifiSchedule, jsonOptions));

            var response = await _apiConnection.ExecuteAsync(req);

            return await Task.FromResult(JsonSerializer.Deserialize<TpLinkResponse<ICollection<WifiSchedule>>>(response.Content));
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