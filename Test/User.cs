using Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    public class User : ApiClient
    {
        private readonly Random _random;
        private readonly object _poolLock;
        private List<Message> _messages;
        private string _login;
        private Timer _timer;
        private bool _lock;

        public User(string login)
        {
            _login = login;
            _random = new Random();
            _poolLock = new object();
            Pool = new List<Task>();
        }

        public int T { get; set; }

        public int Tc { get; set; }

        public async Task Start(CancellationToken token)
        {
            token.Register(Stop);
            await Task.Run(() => _timer = new Timer(Work, null, _random.Next(1, Tc) * 1000, Timeout.Infinite));
        }

        private void Work(object state)
        {
            if (!_lock) // prevents from repeated cross calls
            {
                _lock = true;
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

                try { _timer.Change(_random.Next(1, Tc) * 1000, Timeout.Infinite); }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine($"\rUser {_login} iterator disposed! ({ex.Message})");
                }
                _lock = false;
            }
        }

        private void Update()
        {
            if (_messages == null) // requests initial messages collection for current login
            {
                var t = Task.Run(async () =>
                  {
                      using (var response = await Get("api/client/" + _login))
                          _messages = (await response.Content.ReadAsAsync<IEnumerable<Message>>()).ToList();
                  });
                PoolIn(t);
                t.ContinueWith(antecedent => PoolOut(antecedent)).Wait();
            }

            if (_messages.Count > 0)
            {
                var copy = new Message[_messages.Count];
                _messages.CopyTo(copy);

                Message fresh;
                int i, l;

                for (i = 0, l = copy.Length; i < l; i++)
                {
                    var t = Task.Run(async () =>
                    {
                        var old = copy[i];
                        using (var response = await Get("api/client/" + _login + "/" + old.Id))
                        {
                            fresh = await response.Content.ReadAsAsync<Message>();

                            if (fresh.Finished != null)
                            {
                                _messages.Remove(old);
                                WriteInline(_login + ": message completed id: " + old.Id);
                            }
                            else
                            {
                                _messages[_messages.IndexOf(old)] = fresh;
                                WriteInline(_login + ": message updated: " + old.Id);
                            }
                        }
                    });
                    PoolIn(t);
                    t.ContinueWith(antecedent => PoolOut(antecedent));
                }
            }
        }

        private void New()
        {
            var message = new Message
            {
                Client = _login,
                Contents = DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss ") + "test message from " + _login
            };

            var t = Task.Run(async () =>
            {
                var response = await Post("api/client", message);
                message = await response.Content.ReadAsAsync<Message>();
                _messages.Add(message);
                WriteInline($"{_login} : message created id: {message.Id}");
            });
            PoolIn(t);
            t.ContinueWith(antecedent => PoolOut(antecedent));
        }

        private void Cancel()
        {
            /*var index = _random.Next(0, _messages.Count);
            var message = _messages[index];
            Task.Run(async () =>
            {
                var response = await Put("api/client", message);
                var status = await response.Content.ReadAsAsync<>
                _messages.Add(message);
                WriteInline(_login + ": message cancelled id: " + message.Id);
            }).Wait();*/
        }

        private void PoolIn(Task t) { lock (_poolLock) Pool.Add(t); }
        private void PoolOut(Task t) { lock (_poolLock) Pool.Remove(t); }

        public List<Task> Pool { get; }

        public void Stop() { if (_timer != null) _timer.Dispose(); }

        
    }
}
