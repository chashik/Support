using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace Test
{
    public class ApiClient
    {
        public string ApiHost { get; set; } = "http://localhost";

        protected async Task<HttpResponseMessage> Get(string requestUri)
        {
            using (var httpClient = new HttpClient { BaseAddress = new Uri(ApiHost) })
                return await httpClient.GetAsync(requestUri);
        }

        protected async Task<HttpResponseMessage> Post<T>(string requestUri, T data)
        {
            using (var httpClient = new HttpClient { BaseAddress = new Uri(ApiHost) })
                return await httpClient.PostAsync(requestUri, data, new JsonMediaTypeFormatter());
        }

        protected async Task<HttpResponseMessage> Put<T>(string requestUri, T data)
        {
            using (var httpClient = new HttpClient { BaseAddress = new Uri(ApiHost) })
                return await httpClient.PutAsync(requestUri, data, new JsonMediaTypeFormatter());
        }

        protected void WriteInline(string str) => Console.Write("{0}\r", str);
    }
}
