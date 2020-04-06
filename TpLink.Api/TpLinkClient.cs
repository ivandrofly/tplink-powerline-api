using RestSharp;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using TpLink.Api.Helpers;
using TpLink.Models;

namespace TpLink.Api
{
    public class TpLinkClient : ITpLinkClient
    {
        private readonly RestClient restClient;
        private readonly JsonSerializerOptions _option;

        public TpLinkClient() : this("admin", "admin")
        {

        }

        public TpLinkClient(string login, string password, string endpoint = "http://192.168.1.105/")
        {
            restClient = new RestClient(endpoint)
            {
                UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36", // required
                DefaultParameters =
                {
                    new Parameter("Connection", "keep-alive", ParameterType.HttpHeader),
                    new Parameter("Accept", "application/json, text/javascript, */*; q=0.01", ParameterType.HttpHeader),
                    new Parameter("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8", ParameterType.HttpHeader),
                    new Parameter("Accept-Encoding", "gzip, deflate", ParameterType.HttpHeader),
                    new Parameter("Accept-Language", "en-US,en;q=0.9,pt-PT;q=0.8,pt;q=0.7", ParameterType.HttpHeader),
                    new Parameter("Origin", endpoint, ParameterType.HttpHeader),
                    new Parameter("Referer", endpoint, ParameterType.HttpHeader),
                    new Parameter("X-Requested-With", "XMLHttpRequest", ParameterType.HttpHeader),
                    // todo: compute authorization
                    // The 'Value'='Basic%20admin%3A656a351961b7552d9bb35a0201b6d6fd;path=/
                    //new Parameter("Authorization", "Basic%20admin%3A656a351961b7552d9bb35a0201b6d6fd", ParameterType.Cookie),
                    // undone:
                    new Parameter("Authorization", $"{StringUtils.GetAuthorization(login, password)}", ParameterType.Cookie),
                    new Parameter("DNT", "1", ParameterType.HttpHeader),
                }
            };

            // default option
            _option = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };
        }

        /// <summary>
        /// Get System logs
        /// </summary>
        /// <returns></returns>
        public async Task<TpLinkData<SystemLog>> GetSystemLogsAsync()
        {
            var req = new RestRequest("admin/syslog", Method.POST)
            {
                RequestFormat = DataFormat.None,
                Parameters =
                {
                    new Parameter("form", "log", ParameterType.QueryString),
                    new Parameter("operation", "load", ParameterType.GetOrPost),
                }
            };

            var response = await restClient.ExecuteAsync<TpLinkData<SystemLog>>(req);

            // use system json serializer
            return System.Text.Json.JsonSerializer.Deserialize<TpLinkData<SystemLog>>(response.Content, _option);
        }

        /// <summary>
        /// Get wireless clients connected to the powerline
        /// </summary>
        public async Task<TpLinkClientData> GetClientsAsync()
        {
            var statusRequest = new RestRequest("admin/wireless", Method.POST)
            {
                RequestFormat = DataFormat.None,
                Parameters =
                {
                    new Parameter("form", "statistics", ParameterType.QueryString),
                    new Parameter("operation", "load", ParameterType.GetOrPost),
                }
            };

            // use system json serializer
            var response = await restClient.ExecuteAsync<TpLinkClientData>(statusRequest);
            return System.Text.Json.JsonSerializer.Deserialize<TpLinkClientData>(response.Content, _option);
        }

        /// <summary>
        /// Get powerline status, including transfer and received data rate
        /// </summary>
        public async Task<TpLinkData<SystemLog>> GetPowerlineDevicesStatusAsync()
        {
            var powerLineStatusRequest = new RestRequest("admin/powerline", Method.POST)
            {
                RequestFormat = DataFormat.None,
                Parameters =
                {
                    new Parameter("form", "plc_device", ParameterType.QueryString),
                    new Parameter("operation", "load", ParameterType.GetOrPost),
                }
            };

            // note: deserializing like this ain't working... fix it!
            var response = await restClient.ExecuteAsync<TpLinkData<SystemLog>>(powerLineStatusRequest);
            // use system json serializer
            return System.Text.Json.JsonSerializer.Deserialize<TpLinkData<SystemLog>>(response.Content, _option);
        }

        /// <summary>
        /// Get number of clients currently connected
        /// </summary>
        public async Task<int> GetCountConnectedClientsAsync()
        {
            var clients = await GetClientsAsync();
            return clients.Datas.Count;
        }

        // action method
        // - turn of the wifi (2.4g and 5g)
    }
}
