using Support;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    internal class Employee : ApiClient
    {
        private readonly Random _random;
        private readonly string _login;

        private Timer _timer;
        private bool _aquired;
        private Message _message;

        public Employee(string login)
        {
            _login = login;
            _random = new Random();
        }

        public int Offset { get; set; }

        public int Tmin { get; set; }

        public int Tmax { get; set; }

        public override void Start(CancellationToken token)
        {
            token.Register(() =>
            {
                if (_timer != null) _timer.Dispose();
            });

            _timer = new Timer(Work, null, _random.Next(Tmin, Tmax), Timeout.Infinite);
        }

        private void Work(object state)
        {
            var t = Task.Run(() =>
            {
                if (_aquired)
                {
                    _message.Answer = $"test answer from {_login}";

                    if (Put($"api/messages/{_message.Id}", _message, out HttpStatusCode code))
                    {
                        _aquired = false;
                        WriteInline($"{_login}: message answered (id: {_message.Id})");
                    }
                    else
                        WriteInline($"{_login}: unexpected processing result, HttpStatus: {code}");
                }
                else
                {
                    if (Get($"api/messages/{_login}/{Offset}", out HttpStatusCode code, out _message))
                    {
                        _message.OperatorId = _login;

                        if (Put($"api/messages/{_message.Id}", _message, out code))
                        {
                            _aquired = true;
                            WriteInline($"{_login}: message aquired (id: {_message.Id})");
                        }
                        else
                            WriteInline($"{_login}: Unexpected updating result, HttpStatus: {code}");
                    }
                    else
                        WriteInline($"{_login}: Not aquired, HttpStatus: {code}");
                }

                try
                {
                    _timer.Change(_random.Next(Tmin, Tmax) * 1000, Timeout.Infinite);
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine($"\rOperator {_login} iterator disposed! ({ex.Message})");
                }
            });

            PoolIn(t);
            t.ContinueWith(antecedent => PoolOut(antecedent));
        }
    }
}
