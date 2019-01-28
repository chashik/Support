using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    public abstract class ApiClient : ISimulator
    {
        private readonly object _poolLock;
        private readonly HttpClient _httpClient;
        protected readonly List<Task> _pool;

        protected bool _stopped;

        public ApiClient(string apiHost)
        {
            _poolLock = new object();
            _pool = new List<Task>();
            _httpClient = new HttpClient() { BaseAddress = new Uri(apiHost) };
        }

        protected bool Get<T>(string requestUri, out HttpStatusCode code, out T data)
        {
            try
            {
                var t = _httpClient.GetAsync(requestUri);
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
            catch (ObjectDisposedException ex)
            {
                data = default;
                code = default;
                Console.WriteLine($"{Login}: disposed HttpClient");
                return false;
            }
        }

        public static bool Get<T>(string apiHost, string requestUri, out HttpStatusCode code, out T data)
        {
            using (var httpClient = new HttpClient { BaseAddress = new Uri(apiHost) })
            {
                try
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
                catch (AggregateException ex)
                {
                    Console.WriteLine($"Check connection to host '{apiHost}'");

                    foreach (var v in ex.InnerExceptions)
                    {
                        Console.WriteLine(v.Message);
                        Console.WriteLine(v.GetType());
                    }

                    data = default;
                    code = default;
                    return false;
                }
            }
        }

        protected bool Post<T>(string requestUri, T value, out HttpStatusCode code, out T data)
        {
            try
            {
                var t = _httpClient.PostAsync(requestUri, value, new JsonMediaTypeFormatter());
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
            catch (ObjectDisposedException ex)
            {
                data = default;
                code = default;
                Console.WriteLine($"{Login}: disposed HttpClient");
                return false;
            }
        }


        protected bool Put<T>(string requestUri, T value, out HttpStatusCode code)
        {
            try
            {
                var t = _httpClient.PutAsync(requestUri, value, new JsonMediaTypeFormatter());
                t.Wait();
                using (var response = t.Result)
                {
                    code = response.StatusCode;
                    return code == HttpStatusCode.NoContent;
                }
            }
            catch (ObjectDisposedException ex)
            {

                code = default;
                Console.WriteLine($"{Login}: disposed HttpClient");
                return false;
            }
        }

        public static bool Delete<T>(string apiHost, string requestUri, out HttpStatusCode code, out T data)
        {
            using (var httpClient = new HttpClient { BaseAddress = new Uri(apiHost) })
            {
                var t = httpClient.DeleteAsync(requestUri);
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

        protected void WriteInline(string str)
        {
            Console.Write("\r                                                                 ");
            Console.Write("\r{0}", str);
        }

        protected void PoolIn(Task t) { lock (_poolLock) _pool.Add(t); }

        protected void PoolOut(Task t) { lock (_poolLock) _pool.Remove(t); }

        public virtual void Start(CancellationToken token)
        {
            token.Register(() =>
            {
                _stopped = true;
                Work(null);
            });
        }

        protected abstract void Work(object state);

        public string Login { get; set; } = "ApiClient";

        protected void Dispose() => _httpClient.Dispose();
    }
}
