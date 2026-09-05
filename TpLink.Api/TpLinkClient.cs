using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
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

            // RestClient needs an absolute URL; accept a bare host or IP as a convenience
            if (!apiConnection.Endpoint.Contains("://", StringComparison.Ordinal))
            {
                apiConnection.Endpoint = $"http://{apiConnection.Endpoint}";
            }

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
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
                // note: a custom PropertyNamingPolicy would not help when sending, since requests are form posts, not json
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

        // TODO: remove - use same logic as 5ghz one
        public async Task<TpLinkResponse<WirelessModel>> GetWirelessBand2GAsync()
        {
            var req = new RestRequest("admin/wireless", Method.Post);
            req.AddQueryParameter("form", "wireless_2g");
            req.AddParameter("operation", "read", ParameterType.GetOrPost);
            var res = await _apiConnection.ExecuteAsync(req).ConfigureAwait(false);

            //var jsonObj = new
            //{
            //    name = "ivandro",
            //    age = 102,
            //    order = new object[]
            //    {

            //    }
            //};

            // NOTE: THIS SEEMS TO BE POSSIBLE WITH NETWORNSOFT JSON JProperty
            //var jdoc = JsonDocument.Parse(res.Content);

            //var jdoc = JObject.Parse(res.Content);
            //var r = jdoc.SelectToken("data");

            //jdoc.Root.

            //var jp = jdoc.RootElement.GetProperty("data");

            //req.AddJsonBody(new { name = new { data = ""} });

            //if (jp.GetProperty("enable").GetString().Equals("off"))
            //{
            //    jp.GetProperty("enable");
            //}
            //else
            //{
            //}

            //jdoc.RootElement.GetProperty("enable").

            // todo: still string
            //dynamic instance = JsonSerializer.Deserialize<dynamic>(res.Content);
            //bool success = (bool)instance["success"];

            return JsonSerializer.Deserialize<TpLinkResponse<WirelessModel>>(res.Content, jsonOption);
        }

        public async Task<TpLinkResponse<WirelessModel>> ChangeWireless2GStatusAsync(bool enabled)
        {
            var tpLinkDataWM = await GetWirelessBand2GAsync();
            var req = new RestRequest("admin/wireless", Method.Post);
            req.AddQueryParameter("form", "wireless_2g");
            //req.AddParameter("enable", enabled ? "on" : "off", ParameterType.GetOrPost);
            req.AddParameter("operation", "write", ParameterType.GetOrPost);

            // change the data 
            tpLinkDataWM.Data.Enable = enabled ? "on" : "off";

            // map the prop name and value to a dictionary
            Dictionary<string, string> dictionary = tpLinkDataWM.Data.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(prop =>
                {
                    var jsonProp = prop.GetCustomAttribute<JsonPropertyNameAttribute>(true);
                    return jsonProp == null ? prop.Name.ToLowerInvariant() : jsonProp.Name;
                }, prop => (string)prop.GetValue(tpLinkDataWM.Data));

            // trying to add dictionary with "AddObject" throws exception
            // req.AddObject(dictionary);
            foreach (KeyValuePair<string, string> kvp in dictionary)
            {
                req.AddParameter(kvp.Key, kvp.Value, ParameterType.GetOrPost); // working 
                //req.AddParameter(kvp.Key, kvp.Value);
            }

            // uncommnet line 200 (goto)
            //required_only:
            //    req.AddParameter("enable", enabled ? "on" : "off", ParameterType.GetOrPost);

            // workaround:
            // add by one and convert the prop-name?
            // build a method to handle it all?

            // TODO: CONVERT THE CASING TO "name_name", before sending the post request
            // invoke some type os method to return parameter with corret casing

            await _apiConnection.ExecuteAsync(req).ConfigureAwait(false);
            return null;
        }

        public async Task<TpLinkResponse<WirelessModel>> ChangeWireless5GStatusAsync(bool enabled)
        {
            // send request to retrive the currnet wifi password
            var reqStatus = new RestRequest("admin/wlan_status", Method.Post);
            reqStatus.AddParameter("operation", "read", ParameterType.GetOrPost);
            var resStatus = await _apiConnection.ExecuteAsync(reqStatus).ConfigureAwait(false);

            // operation failed
            if (resStatus.IsSuccessful == false)
            {
                return null;
            }

            var jdoc = JsonDocument.Parse(resStatus.Content, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

            // get pre-shared key for 5ghz network
            string pskKey = jdoc.RootElement
                .GetProperty("data")
                .GetProperty("wireless_5g_pwd").GetString();

            string ssid5ghz = jdoc.RootElement
                .GetProperty("data")
                .GetProperty("wireless_5g_ssid").GetString();

            string encryption = jdoc.RootElement
                .GetProperty("data")
                .GetProperty("wireless_5g_encryption").GetString();

            // build request to update 5ghz wireless
            var req = new RestRequest("admin/wireless", Method.Post);
            req.AddQueryParameter("form", "wireless_5g");
            req.AddParameter("operation", "write", ParameterType.GetOrPost);
            req.AddParameter("enable", enabled ? "on" : "off", ParameterType.GetOrPost);
            req.AddParameter("ssid", ssid5ghz, ParameterType.GetOrPost);
            req.AddParameter("hidden", "off", ParameterType.GetOrPost);
            req.AddParameter("psk_key", pskKey, ParameterType.GetOrPost);
            req.AddParameter("encryption", encryption, ParameterType.GetOrPost);
            req.AddParameter("psk_version", "auto", ParameterType.GetOrPost);
            req.AddParameter("psk_cipher", "auto", ParameterType.GetOrPost);
            req.AddParameter("hwmode", "a", ParameterType.GetOrPost);
            req.AddParameter("htmode", "80", ParameterType.GetOrPost);
            req.AddParameter("channel", "auto", ParameterType.GetOrPost);
            req.AddParameter("txpower", "low", ParameterType.GetOrPost);

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
        /// Find out which ip address is assigned to the powerline by broadcasting a UDP discovery
        /// message and waiting for the first adapter that answers.
        /// </summary>
        /// <param name="timeout">How long to wait for an answer. Defaults to 5 seconds.</param>
        /// <returns>The ip address of the powerline</returns>
        /// <exception cref="TimeoutException">No adapter answered within <paramref name="timeout"/>.</exception>
        public static async Task<string> DiscoveryAsync(TimeSpan? timeout = null)
        {
            var wait = timeout ?? TimeSpan.FromSeconds(5);

            // note here 192.168.1.86 was the ip that powerline was using - this is dynamic can change
            // wireshark filter: (ip.dst == 192.168.1.108 && ip.src == 192.168.1.86 ) || (ip.dst == 255.255.255.255)
            using var uc = new UdpClient(new IPEndPoint(IPAddress.Any, 61000))
            {
                EnableBroadcast = true,
            };

            // the binary discovery frame captured in wireshark is not needed; a plain text broadcast gets an answer
            var buffer = Encoding.UTF8.GetBytes("Where are you!");

            // note: i think this is not really safe, because if powerline was fast enough the receive won't be able to capture
            // see: https://stackoverflow.com/a/40617102/2766753 for more relaiable udp implementation
            await uc.SendAsync(buffer, buffer.Length, IPAddress.Broadcast.ToString(), 1040).ConfigureAwait(false);

            using var cts = new CancellationTokenSource(wait);
            try
            {
                UdpReceiveResult udpResponse = await uc.ReceiveAsync(cts.Token).ConfigureAwait(false);

                // get the discovered ip addres of the powerline
                return udpResponse.RemoteEndPoint.Address.ToString();
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException(
                    $"No powerline adapter answered the discovery broadcast within {wait.TotalSeconds:0.#} seconds. " +
                    "Discovery does not work through a VPN and needs inbound UDP port 61000 open; " +
                    "alternatively pass the adapter's IP to the TpLinkClient constructor.");
            }
        }

        public Task<object> AddNewUserAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<TpLinkResponse<ICollection<WifiSchedule>>> AddNewWifiScheduleAsync(WifiSchedule wifiSchedule)
        {
            // validation
            if (wifiSchedule is null)
            {
                throw new ArgumentNullException(nameof(wifiSchedule));
            }

            if (wifiSchedule.StartTime < 0 || wifiSchedule.StartTime > 24)
            {
                throw new ArgumentOutOfRangeException(nameof(wifiSchedule), wifiSchedule.StartTime,
                    $"{nameof(WifiSchedule.StartTime)} must be between 0 and 24.");
            }

            if (wifiSchedule.EndTime < 0 || wifiSchedule.EndTime > 24)
            {
                throw new ArgumentOutOfRangeException(nameof(wifiSchedule), wifiSchedule.EndTime,
                    $"{nameof(WifiSchedule.EndTime)} must be between 0 and 24.");
            }

            if (wifiSchedule.StartTime >= wifiSchedule.EndTime)
            {
                throw new ArgumentException(
                    $"{nameof(WifiSchedule.StartTime)} must be earlier than {nameof(WifiSchedule.EndTime)}.",
                    nameof(wifiSchedule));
            }

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
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
    }
}