using Support;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    internal class Operator : ApiClient, ISimulator
    {
        private readonly Random _random;
        private readonly object _poolLock;
        private readonly string _login;

        private Timer _timer;
        private bool _aquired;
        private Message _message;

        public Operator(string login)
        {
            _login = login;
            _random = new Random();
            _poolLock = new object();
            Pool = new List<Task>();
        }

        public int Offset { get; set; }

        public int Tmin { get; set; }

        public int Tmax { get; set; }

        public void Start(CancellationToken token)
        {
            token.Register(Stop);
            _timer = new Timer(Work, null, _random.Next(Tmin, Tmax), Timeout.Infinite);
        }

        private void Work(object state)
        {
            Task _work;
            if (_aquired)
            {
                _work = Task.Run(async () =>
                 {
                     _message.Answer = $"test answer from {_login}";
                     using (var response = await Put($"api/client/{_message.Id}", _message))
                     {
                         var status = response.StatusCode;
                         if (status == HttpStatusCode.NoContent)
                         {
                             _aquired = false;
                             WriteInline($"{_login}: message answered (id: {_message.Id})");
                         }
                         else
                             WriteInline($"{_login}: unexpected result, HttpStatus: {status}");
                     }
                 });
            }
            else
            {
                _work = Task.Run(async () =>
                  {
                      var response = await Get($"api/client/{_login}/{Offset}");
                      {
                          var status = response.StatusCode;
                          if (status == HttpStatusCode.OK)
                          {
                              _message = await response.Content.ReadAsAsync<Message>();
                              _message.OperatorId = _login;
                              var response2 = await Put($"api/client/{_message.Id}", _message);
                              {
                                  status = response2.StatusCode;
                                  if (status == HttpStatusCode.NoContent)
                                  {
                                      _aquired = true;
                                      WriteInline($"{_login}: message aquired (id: {_message.Id})");
                                  }
                                  else
                                      WriteInline($"{_login}: Unexpected result, HttpStatus: {status}");
                              }
                              response2.Dispose();
                          }
                          else
                              WriteInline($"{_login}: Unexpected result, HttpStatus: {status}");
                      }
                      response.Dispose();
                  });
            }

            PoolIn(_work);
            _work.ContinueWith(antecedent => PoolOut(antecedent));

            try { _timer.Change(_random.Next(Tmin, Tmax) * 1000, Timeout.Infinite); }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"\rOperator {_login} iterator disposed! ({ex.Message})");
            }
        }

        private void PoolIn(Task t) { lock (_poolLock) Pool.Add(t); }
        private void PoolOut(Task t) { lock (_poolLock) Pool.Remove(t); }

        public List<Task> Pool { get; }

        private void Stop() { if (_timer != null) _timer.Dispose(); }
    }
}
