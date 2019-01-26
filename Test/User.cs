using Support;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    public class User : ApiClient, ISimulator
    {
        private readonly object _poolLock;
        private readonly Random _random;

        private ConcurrentBag<Message> _messages;
        private string _login;
        private Timer _timer;
        private bool _workLock;

        public User(string login)
        {
            _login = login;
            _random = new Random();
            _poolLock = new object();
            Pool = new List<Task>();
        }

        public int T { get; set; }

        public int Tc { get; set; }

        public void Start(CancellationToken token)
        {
            token.Register(Stop);
            _timer = new Timer(Work, null, _random.Next(T, Tc), Timeout.Infinite);
        }

        private void Work(object state)
        {
            if (!_workLock) // repeated cross calls are omitted
            {
                _workLock = true;

                Update(); // checks status and updates messages list for current login

                if (_messages.Count > 0) // "flips a coin" to choose whether to create new message or cancel an older one
                {
                    var coin = Convert.ToBoolean(_random.Next(0, 2));

                    if (coin)
                        New();
                    else
                        Cancel();
                }
                else // creates new message
                    New();

                try
                {
                    _timer.Change(_random.Next(T, Tc) * 1000, Timeout.Infinite);
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine($"\rUser {_login} iterator disposed! ({ex.Message})");
                }

                _workLock = false;
            }
        }

        private void Update()
        {
            if (_messages == null) // requests initial messages collection for current login
            {
                if (Get($"api/client/{_login}", out HttpStatusCode code, out IEnumerable<Message> messages))
                {
                    _messages = new ConcurrentBag<Message>(messages);
                    WriteInline($"{_login}: {_messages.Count} unanswered messages loaded");
                }
                else
                    WriteInline($"{_login}: unexpected result, HttpStatus: {code} (initial collection)");
            }

            if (_messages.Count > 0)
            {
                var updated = new List<Message>();
                while (_messages.TryTake(out Message message))
                {
                    if (Get($"api/client/{_login}/{message.Id}", out HttpStatusCode code, out Message fresh))
                    {
                        if (fresh.Finished != null)
                            WriteInline($"{_login}: message completed (id: {message.Id})");
                        else
                        {
                            updated.Add(fresh);
                            WriteInline($"{_login}: message updated (id: {message.Id})");
                        }
                    }
                    else
                        WriteInline($"{_login}: unexpected updating result, HttpStatus: {code}");
                }
                _messages = new ConcurrentBag<Message>(updated.Distinct());
            }
        }

        private void New()
        {
            var message = new Message
            {
                Client = _login,
                Contents = $"{DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss ")} test message from {_login}"
            };

            var t = Task.Run(() =>
            {
                if (Post("api/client", message, out HttpStatusCode code, out message))
                {
                    _messages.Add(message);
                    WriteInline($"{_login} : message created (id: {message.Id})");
                }
                else
                    WriteInline($"{_login}: unexpected creation result, HttpStatus: {code}");
            });
            PoolIn(t);
            t.ContinueWith(antecedent => PoolOut(antecedent));
        }

        private void Cancel()
        {
            if (_messages.TryTake(out Message message))
            {
                var copy = message.ShallowCopy();
                copy.Cancelled = true;

                var t = Task.Run(() =>
                {
                    if (Put($"api/client/{message.Id}", copy, out HttpStatusCode code))
                        WriteInline($"{ _login}: message cancelled (id: {message.Id})");
                    else
                    {
                        _messages.Add(message);
                        WriteInline($"{_login}: unexpected cancellation result, HttpStatus: {code}");
                    }
                });

                PoolIn(t);
                t.ContinueWith(antecedent => PoolOut(antecedent));
            }
        }

        private void PoolIn(Task t) { lock (_poolLock) Pool.Add(t); }

        private void PoolOut(Task t) { lock (_poolLock) Pool.Remove(t); }

        public List<Task> Pool { get; }

        private void Stop() { if (_timer != null) _timer.Dispose(); }

        
    }
}
