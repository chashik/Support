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
        protected readonly Random _random;

        private bool _stopped;
        private Timer _timer;

        public ApiClient(string apiHost)
        {
            _poolLock = new object();
            _pool = new List<Task>();
            _httpClient = new HttpClient() { BaseAddress = new Uri(apiHost) };
            _random = new Random();
        }

        public string Login { get; set; } = "ApiClient";

        public int Tmin { get; set; }

        public int Tmax { get; set; }

        protected void WriteInline(string str)
        {
            Console.Write("\r                                                                 ");
            Console.Write("\r{0}", str);
        }

        // PUBLIC BEHAVIOR (Single responsibility)
        #region
        public virtual void Start(CancellationToken token)
        {
            token.Register(() =>
            {
                _stopped = true;
                Do(null);
            });

            _timer = new Timer(Do, null, _random.Next(Tmin, Tmax), Timeout.Infinite);
        }

        private void Do(object stopped)
        {
            if (_stopped)
            {
                if (_timer != null) // cancel iteration
                    _timer.Dispose();

                while (_pool.Count > 0) // waiting for HttpClient to finish requests
                    Thread.Sleep(1000);

                _httpClient.Dispose();
            }
            else // do work and plan next
            {
                var t = new Task(Work);
                _pool.Add(t);
                t.ContinueWith(antecedent =>
                {
                    _pool.Remove(antecedent);
                    try
                    {
                        _timer.Change(_random.Next(Tmin, Tmax) * 1000, Timeout.Infinite);
                    }
                    catch (ObjectDisposedException ex)
                    {
                        Console.WriteLine($"\rUser {Login} iterator disposed! ({ex.Message})");
                    }
                });
                t.Start();
            }
        }

        protected abstract void Work();
        #endregion

        // HTTP METHODS
        #region
        protected bool Get<T>(string requestUri, out HttpStatusCode code, out T data)
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


        protected bool Put<T>(string requestUri, T value, out HttpStatusCode code)
        {
            var t = _httpClient.PutAsync(requestUri, value, new JsonMediaTypeFormatter());
            t.Wait();
            using (var response = t.Result)
            {
                code = response.StatusCode;
                return code == HttpStatusCode.NoContent;
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
        #endregion
    }
}
