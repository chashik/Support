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

        private Timer _timer;
        private bool _aquired;
        private Message _message;

        public Employee(string apiHost) : base(apiHost) => _random = new Random();

        public int Offset { get; set; }

        public int Tmin { get; set; }

        public int Tmax { get; set; }

        public override void Start(CancellationToken token)
        {
            base.Start(token);

            _timer = new Timer(Work, null, _random.Next(Tmin, Tmax), Timeout.Infinite);
        }

        protected override void Work(object state)
        {
            if (_stopped)
            {
                if (_timer != null) _timer.Dispose();
                while (_pool.Count > 0)
                    Thread.Sleep(1000);
                Dispose();
            }
            else
            {
                var t = Task.Run(() =>
                {
                    if (_aquired) // answer message, put it to server and wait for timer
                {
                        _message.Answer = $"test answer from {Login}";

                        if (Put($"api/messages/{_message.Id}", _message, out HttpStatusCode code))
                        {
                            _aquired = false;
                            WriteInline($"{Login}: message answered (id: {_message.Id})");
                        }
                        else
                            WriteInline($"{Login}: unexpected processing result, HttpStatus: {code}");
                    }
                    else // aquire message from server and wait for timer
                {
                        if (Get($"api/messages/{Login}/{Offset}", out HttpStatusCode code, out _message))
                        {
                            _message.OperatorId = Login;

                            if (Put($"api/messages/{_message.Id}", _message, out code))
                            {
                                _aquired = true;
                                WriteInline($"{Login}: message aquired (id: {_message.Id})");
                            }
                            else
                                WriteInline($"{Login}: Unexpected updating result, HttpStatus: {code}");
                        }
                        else
                            WriteInline($"{Login}: Not aquired, HttpStatus: {code}");
                    }

                    try
                    {
                        _timer.Change(_random.Next(Tmin, Tmax) * 1000, Timeout.Infinite);
                    }
                    catch (ObjectDisposedException ex)
                    {
                        Console.WriteLine($"\rOperator {Login} iterator disposed! ({ex.Message})");
                    }
                });

                PoolIn(t);
                t.ContinueWith(antecedent => PoolOut(antecedent));
            }
        }
    }
}
