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
        private bool _workLock;
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
            if (!_workLock) // repeated cross calls are omitted
            {
                _workLock = true;

                Task work;

                if (_aquired)
                {
                    work = Task.Run(() =>
                     {
                         _message.Answer = $"test answer from {_login}";

                         if (Put($"api/client/{_message.Id}", _message, out HttpStatusCode code))
                         {
                             _aquired = false;
                             WriteInline($"{_login}: message answered (id: {_message.Id})");
                         }
                         else
                             WriteInline($"{_login}: unexpected processing result, HttpStatus: {code}");
                     });
                }
                else
                {
                    work = Task.Run(() =>
                    {
                        if (Get($"api/client/{_login}/{Offset}", out HttpStatusCode code, out _message))
                        {
                            _message.OperatorId = _login;

                            if (Put($"api/client/{_message.Id}", _message, out code))
                            {
                                _aquired = true;
                                WriteInline($"{_login}: message aquired (id: {_message.Id})");
                            }
                            else
                                WriteInline($"{_login}: Unexpected updating result, HttpStatus: {code}");
                        }
                        else
                            WriteInline($"{_login}: Unexpected aquiring result, HttpStatus: {code}");
                    });
                }

                PoolIn(work);
                work.ContinueWith(antecedent => PoolOut(antecedent));

                try { _timer.Change(_random.Next(Tmin, Tmax) * 1000, Timeout.Infinite); }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine($"\rOperator {_login} iterator disposed! ({ex.Message})");
                }

                _workLock = false;
            }
        }

        private void PoolIn(Task t) { lock (_poolLock) Pool.Add(t); }
        private void PoolOut(Task t) { lock (_poolLock) Pool.Remove(t); }

        public List<Task> Pool { get; }

        private void Stop() { if (_timer != null) _timer.Dispose(); }
    }
}
