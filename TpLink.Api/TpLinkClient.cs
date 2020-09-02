using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TpLink.Api.Helpers;
using TpLink.Api.Models;
using TpLink.Models;

namespace TpLink.Api
{
    public class TpLinkClient : ITpLinkClient
    {
        private readonly RestClient restClient;
        private readonly JsonSerializerOptions jsonOption;

        public TpLinkClient() : this("admin", "admin", "192.168.1.1")
        {
        }

        [Obsolete]
        public TpLinkClient(string login, string password, string endpoint)
        {
            restClient = new RestClient(endpoint)
            {
                // NOTE: Whne the version of the user agent change, this may need to be changed aswell
                // the entire request may be okay, but when the user agent's version changed, this may need to be updated aswell
                // Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.163 Safari/537.36
                // important: setting rule for user-agent in fiddler will override this, which can cause several complication
                //UserAgent = @"TPlinkClient", // required
                UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.163 Safari/537.36", // required

                DefaultParameters =
                {
                    new Parameter("Connection", "keep-alive", ParameterType.HttpHeader),
                    new Parameter("Accept", "application/json, text/javascript, */*; q=0.01", ParameterType.HttpHeader),
                    new Parameter("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8", ParameterType.HttpHeader),
                    new Parameter("Accept-Encoding", "gzip,deflate", ParameterType.HttpHeader),
                    new Parameter("Accept-Language", "en-US,en;q=0.9,pt-PT;q=0.8,pt;q=0.7", ParameterType.HttpHeader),
                    new Parameter("Origin", endpoint, ParameterType.HttpHeader),
                    new Parameter("Referer", endpoint, ParameterType.HttpHeader),
                    new Parameter("X-Requested-With", "XMLHttpRequest", ParameterType.HttpHeader),
                    // todo: compute authorization
                    // The 'Value'='Basic%20admin%3A656a351961b7552d9bb35a0201b6d6fd;path=/
                    // new Parameter("Authorization", "Basic%20admin%3A656a351961b7552d9bb35a0201b6d6fd", ParameterType.Cookie),
                    new Parameter("Authorization", $"{StringUtils.GetAuthorization(login, password)}", ParameterType.Cookie),
                    new Parameter("DNT", "1", ParameterType.HttpHeader),
                },

                Timeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds
            };

            // default option
            jsonOption = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                // note: wont work when sending the request, since the request ain't sent as json! :(
                //PropertyNamingPolicy = new TpLinkPropertyNamingPolicy(),
            };

            // Ignore for now! tplink server returns wron'g content-type
            //restClient.UseSystemTextJson(_option);
        }

        /// <summary>
        /// Get System logs
        /// </summary>
        public async Task<TpLinkResponse<List<SystemLog>>> GetSystemLogsAsync()
        {
            // build the system log
            var req = new RestRequest("admin/syslog", Method.POST)
            {
                RequestFormat = DataFormat.None,
                Parameters =
                {
                    new Parameter("form", "log", ParameterType.QueryString),
                    new Parameter("operation", "load", ParameterType.GetOrPost),
                }
            };

            // rest thee response from the http response and try to parse it.
            // note: since the response comes wiht "content-type: text/html" it json parser will fail to parse it
            var response = await restClient.ExecuteAsync(req);
            // use system json serializer
            var instance = JsonSerializer.Deserialize<TpLinkResponse<List<SystemLog>>>(response.Content, jsonOption);
            return instance ?? new TpLinkResponse<List<SystemLog>>();
        }

        /// <summary>
        /// Get wireless clients connected to the powerline
        /// </summary>
        //public async Task<TpLinkClientData> GetClientsAsync()
        public async Task<TpLinkClientData> GetClientsAsync()
        {
            var wirelessClientReq = new RestRequest("admin/wireless", Method.POST)
            {
                RequestFormat = DataFormat.None,
                Parameters =
                {
                    new Parameter("form", "statistics", ParameterType.QueryString),
                    new Parameter("operation", "load", ParameterType.GetOrPost),
                },
            };

            //restClient.AddHandler("text/html", () => new JsonSerializer(_option));
            //restClient.RemoveHandler("text/html"); // exception
            // use system json serializer
            // IMPORTANT: TP-LINK SERVER DOESN'T RETURN THE CORRECT CONTENT TYPE WHICH
            // MAKE THE JSONSERIALIZER TO USE THE XML BY DEFAULT
            IRestResponse response = await restClient.ExecuteAsync(wirelessClientReq);

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
        public async Task<TpLinkResponse<PowerlineDevices>> GetPowerlineDevicesStatusAsync()
        {
            var powerLineStatusRequest = new RestRequest("admin/powerline", Method.POST)
            {
                RequestFormat = DataFormat.None,

                // deprecated
                //Parameters =
                //{
                //    new Parameter("form", "plc_device", ParameterType.QueryString),
                //    new Parameter("operation", "load", ParameterType.GetOrPost),
                //}
            };

            powerLineStatusRequest.AddQueryParameter("form", "plc_device");
            powerLineStatusRequest.AddParameter("operation", "load", ParameterType.GetOrPost);

            // IMPORTANT: TP-LINK SERVER RETURNS WRONG CONTENT TYPE (TEXT/HTML) WHICH INVOKES XML SERIALIZER BY DEFAULT
            //var response = await restClient.ExecuteAsync<TpLinkData<SystemLog>>(powerLineStatusRequest);

            var response = await restClient.ExecuteAsync(powerLineStatusRequest);
            return JsonSerializer.Deserialize<TpLinkResponse<PowerlineDevices>>(response.Content, jsonOption);
        }

        /// <summary>
        /// Get number of clients currently connected
        /// </summary>
        public async Task<int> GetCountConnectedClientsAsync()
        {
            var clients = await GetClientsAsync();
            return clients.Data.Count;
        }

        public async Task<TpLinkResponse<WirelessModel>> GetWirelessBand2GAsync()
        {
            var req = new RestRequest("admin/wireless", Method.POST);
            req.AddQueryParameter("form", "wireless_2g");
            req.AddParameter("operation", "read", ParameterType.GetOrPost);
            var res = await restClient.ExecuteAsync(req);

            // NOTE: THIS SEEMS TO BE POSSIBLE WITH NETWORNSOFT JSON JProperty
            var jdoc = JsonDocument.Parse(res.Content);

            //var jdoc = JObject.Parse(res.Content);
            //var r = jdoc.SelectToken("data");

            //jdoc.Root.

            var jp = jdoc.RootElement.GetProperty("data");

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
            var req = new RestRequest("admin/wireless", Method.POST)
            {
                //RequestFormat = DataFormat.Json
                //Parameters =
                //{
                //    new Parameter("form", "wireless_2g", ParameterType.QueryString),
                //    new Parameter("enable", "write", ParameterType.GetOrPost)
                //}
            };

            req.AddQueryParameter("form", "wireless_2g");
            //req.AddParameter("enable", enabled ? "on" : "off", ParameterType.GetOrPost);
            req.AddParameter("operation", "write", ParameterType.GetOrPost);

            //goto required_only;

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

            // TODO: CONVERT THE CASING TO "name_name", before sendin the post request
            // invoke some type os method to return parameter with corret casing

            var res = await restClient.ExecuteAsync(req);
            return null;
        }

        public async Task<TpLinkResponse<WirelessModel>> ChangeWireless5GStatusAsync(bool enabled)
        {
            var req = new RestRequest("admin/wireless", Method.POST);
            req.AddQueryParameter("form", "wireless_5g");

            req.AddParameter("operation", "write", ParameterType.GetOrPost);
            req.AddParameter("enable", enabled ? "on" : "off", ParameterType.GetOrPost);
            req.AddParameter("ssid", "TP-LINK_2BDA_5G", ParameterType.GetOrPost);
            req.AddParameter("hidden", "off", ParameterType.GetOrPost);
            req.AddParameter("psk_key", "93608305", ParameterType.GetOrPost);
            req.AddParameter("encryption", "psk", ParameterType.GetOrPost);
            req.AddParameter("psk_version", "auto", ParameterType.GetOrPost);
            req.AddParameter("psk_cipher", "auto", ParameterType.GetOrPost);
            req.AddParameter("hwmode", "a", ParameterType.GetOrPost);
            req.AddParameter("htmode", "80", ParameterType.GetOrPost);
            req.AddParameter("channel", "auto", ParameterType.GetOrPost);
            req.AddParameter("txpower", "low", ParameterType.GetOrPost);
            var res = await restClient.ExecuteAsync(req);
            return JsonSerializer.Deserialize<TpLinkResponse<WirelessModel>>(res.Content, jsonOption);
        }

        public async Task<TpLinkResponse<WifiMove>> WifiMoveAsync(bool enabled)
        {
            var req = new RestRequest("/admin/wifiMove.json", Method.POST)
            {
                Parameters =
                {
                    new Parameter("operation", "write", ParameterType.GetOrPost),
                    new Parameter("enable", enabled ? 1 : 0, ParameterType.GetOrPost),
                }
            };
            //req.AddParameter("operation", "read");

            // TODO: NOT WORKING, BUT THE REQUEST LOOKS THE SAME AS FROM CHROME BROWSER!
            var res = await restClient.ExecuteAsync(req);
            var data = JsonSerializer.Deserialize<TpLinkResponse<WifiMove>>(res.Content, jsonOption);
            return data;
        }

        public async Task<TpLinkResponse<bool>> RebootAsync()
        {
            var req = new RestRequest("/admin/reboot.json", Method.POST)
            {
                RequestFormat = DataFormat.None,
                Parameters =
                {
                    new Parameter("operation", "write", ParameterType.GetOrPost),
                }
            };

            var response = await restClient.ExecuteAsync(req);
            return await Task.FromResult(new TpLinkResponse<bool> { Data = true });
        }

        // action method
        // turn off the wifi (2.4g and 5g)
    }
}