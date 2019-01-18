using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Support;

namespace Test
{
    public abstract class ApiClient
    {

        protected readonly string _apiHost;
        protected readonly int _clients;

        protected MyConfig _myConfig;

        public ApiClient(IConfigurationRoot conf)
        {
            _apiHost = conf.GetValue<string>("ApiHost");
            _clients = conf.GetValue<int>("Clients");
        }

        protected async Task ApiConf()
        {
            using (var response = await Get("api/config"))
                _myConfig = await response.Content.ReadAsAsync<MyConfig>();
        }

        protected async Task<HttpResponseMessage> Get(string requestUri)
        {
            return await Get(_apiHost, requestUri);
        }

        public static async Task<HttpResponseMessage> Get(string baseUri, string requestUri)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(baseUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                return await httpClient.GetAsync(requestUri);
            }
        }
    }
}
