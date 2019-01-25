using Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    public class User : ApiClient, ISimulator
    {
        private readonly object _poolLock;
        private readonly Random _random;

        private List<Message> _messages;
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
                var t = Task.Run(async () =>
                {
                    using (var response = await Get($"api/client/{_login}"))
                    {
                        var status = response.StatusCode;
                        if (status == HttpStatusCode.OK)
                            _messages = (await response.Content.ReadAsAsync<IEnumerable<Message>>()).ToList();
                        else
                            WriteInline($"{_login}: unexpected result, HttpStatus: {status} (initial collection)");
                    }
                });

                PoolIn(t);
                t.ContinueWith(antecedent => PoolOut(antecedent));
                t.Wait(); // waiting while collection is prepared
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
                        using (var response = await Get($"api/client/{_login}/{old.Id}"))
                        {
                            var status = response.StatusCode;
                            if (status == HttpStatusCode.OK)
                            {
                                fresh = await response.Content.ReadAsAsync<Message>();

                                if (fresh.Finished != null)
                                {
                                    _messages.Remove(old);
                                    WriteInline($"{_login}: message completed (id: {old.Id})");
                                }
                                else
                                {
                                    _messages[_messages.IndexOf(old)] = fresh;
                                    WriteInline($"{_login}: message updated (id: {old.Id})");
                                }
                            }
                            else
                                WriteInline($"{_login}: unexpected result, HttpStatus: {status}");
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
                using (var response = await Post("api/client", message))
                {
                    var status = response.StatusCode;
                    if (status == HttpStatusCode.Created)
                    {
                        message = await response.Content.ReadAsAsync<Message>();
                        _messages.Add(message);
                        WriteInline($"{_login} : message created (id: {message.Id})");
                    }
                    else
                        WriteInline($"{_login}: unexpected result, HttpStatus: {status}");
                }
            });
            PoolIn(t);
            t.ContinueWith(antecedent => PoolOut(antecedent));
        }

        private void Cancel()
        {
            var index = _random.Next(0, _messages.Count);
            var message = _messages[index];
            var copy = message.ShallowCopy();
            copy.Cancelled = true;

            Task.Run(async () =>
            {
                using (var response = await Put($"api/client/{message.Id}", copy))
                {
                    var status = response.StatusCode;
                    if (status == HttpStatusCode.NoContent)
                    {
                        _messages.Remove(message);
                        WriteInline($"{ _login}: message cancelled (id: {message.Id})");
                    }
                    else
                        WriteInline($"{_login}: unexpected result, HttpStatus: {status}");
                }
            });
        }

        private void PoolIn(Task t) { lock (_poolLock) Pool.Add(t); }

        private void PoolOut(Task t) { lock (_poolLock) Pool.Remove(t); }

        public List<Task> Pool { get; }

        private void Stop() { if (_timer != null) _timer.Dispose(); }

        
    }
}
