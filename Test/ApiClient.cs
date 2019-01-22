using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Test
{
    public abstract class ApiClient
    {

        protected readonly string _apiHost;

        public ApiClient(string apiHost)
        {
            _apiHost = apiHost;
        }

        protected async Task<HttpResponseMessage> Get(string requestUri)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiHost);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                return await httpClient.GetAsync(requestUri);
            }
            //return await Get(_apiHost, requestUri);
        }

        /*protected static async Task<HttpResponseMessage> Get(string baseUri, string requestUri)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(baseUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                return await httpClient.GetAsync(requestUri);
            }
        }*/
    }


}
