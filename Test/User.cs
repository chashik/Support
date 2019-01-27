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
    public class User : ApiClient
    {
        private readonly Random _random;

        private ConcurrentBag<Message> _messages;
        private Timer _timer;

        public User(string apiHost) : base(apiHost) => _random = new Random();

        public int T { get; set; }

        public int Tc { get; set; }

        public override void Start(CancellationToken token)
        {
            token.Register(() =>
            {
                if (_timer != null) _timer.Dispose();
                Dispose();
            });

            _timer = new Timer(Work, null, _random.Next(T, Tc), Timeout.Infinite);
        }

        private void Work(object state)
        {
            Update(); // checks status and updates messages list for current login synchronously (almost)

            var t = Task.Run(() =>
            {
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
                    Console.WriteLine($"\rUser {Login} iterator disposed! ({ex.Message})");
                }
            });

            PoolIn(t);
            t.ContinueWith(antecedent => PoolOut(antecedent));
        }

        private void Update()
        {
            if (_messages == null) // requests initial messages collection for current login
            {
                if (Get($"api/messages/{Login}", out HttpStatusCode code, out IEnumerable<Message> messages))
                {
                    _messages = new ConcurrentBag<Message>(messages);
                    WriteInline($"{Login}: {_messages.Count} unanswered messages loaded");
                }
                else
                    WriteInline($"{Login}: unexpected result, HttpStatus: {code} (initial collection)");
            }

            if (_messages.Count > 0)
            {
                var updated = new ConcurrentBag<Message>();
                var messages = new List<Message>();
                var t = new List<Task>();

                while (_messages.TryTake(out Message message))
                    messages.Add(message);

                foreach(var message in messages)
                    t.Add(Task.Run(() => 
                    {
                        if (Get($"api/messages/{Login}/{message.Id}", out HttpStatusCode code, out Message fresh))
                        {
                            if (fresh.Finished != null)
                                WriteInline($"{Login}: message completed (id: {message.Id})");
                            else
                            {
                                updated.Add(fresh);
                                WriteInline($"{Login}: message updated (id: {message.Id})");
                            }
                        }
                        else
                            WriteInline($"{Login}: unexpected updating result, HttpStatus: {code}");
                    }));

                messages.Clear();
                Task.WaitAll(t.ToArray());
                _messages = new ConcurrentBag<Message>(updated.Distinct());
                updated.Clear();
            }
        }

        private void New()
        {
            var message = new Message
            {
                Client = Login,
                Contents = $"{DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss ")} test message from {Login}"
            };

            if (Post("api/messages", message, out HttpStatusCode code, out message))
            {
                _messages.Add(message);
                WriteInline($"{Login} : message created (id: {message.Id})");
            }
            else
                WriteInline($"{Login}: unexpected creation result, HttpStatus: {code}");
        }

        private void Cancel()
        {
            if (_messages.TryTake(out Message message))
            {
                var copy = message.ShallowCopy();
                copy.Cancelled = true;


                if (Put($"api/messages/{message.Id}", copy, out HttpStatusCode code))
                    WriteInline($"{ Login}: message cancelled (id: {message.Id})");
                else
                {
                    _messages.Add(message);
                    WriteInline($"{Login}: unexpected cancellation result, HttpStatus: {code}");
                }
            }
        }
    }
}
