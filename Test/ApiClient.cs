using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;

namespace Test
{
    public class ApiClient
    {
        public string ApiHost { get; set; } = "http://localhost";

        protected bool Get<T>(string requestUri, out HttpStatusCode code, out T data)
        {
            using (var httpClient = new HttpClient { BaseAddress = new Uri(ApiHost) })
            {
                var t = httpClient.GetAsync(requestUri);
                t.Wait();
                using (var response = t.Result)
                {
                    code = response.StatusCode;
                    if (code == HttpStatusCode.OK)
                    {
                        var t1 = response.Content.ReadAsAsync<T>();
                        t1.Wait();
                        data = t1.Result;
                        return true;
                    }
                    else
                    {
                        data = default;
                        return false;
                    }
                }
            }
        }

        protected bool Post<T>(string requestUri, T value, out HttpStatusCode code, out T data)
        {
            using (var httpClient = new HttpClient { BaseAddress = new Uri(ApiHost) })
            {
                var t = httpClient.PostAsync(requestUri, value, new JsonMediaTypeFormatter());
                t.Wait();
                using (var response = t.Result)
                {
                    code = response.StatusCode;
                    if (code == HttpStatusCode.Created)
                    {
                        var t1 = response.Content.ReadAsAsync<T>();
                        t1.Wait();
                        data = t1.Result;
                        return true;
                    }
                    else
                    {
                        data = default;
                        return false;
                    }
                }
            }
        }

        protected bool Put<T>(string requestUri, T value, out HttpStatusCode code)
        {
            using (var httpClient = new HttpClient { BaseAddress = new Uri(ApiHost) })
            {
                var t = httpClient.PutAsync(requestUri, value, new JsonMediaTypeFormatter());
                t.Wait();
                using (var response = t.Result)
                {
                    code = response.StatusCode;
                    return code == HttpStatusCode.NoContent;
                }
            }
        }

        protected void WriteInline(string str)
        {
            Console.Write("\r                                                                 ");
            Console.Write("\r{0}", str);
        }
    }
}
