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

        private List<Message> _messages;
        private string _login;

        public User(string login)
        {
            _login = login;
            _random = new Random();
        }



        public int Interval { get; set; } = 20;

        public async Task Start(CancellationToken token)
        {
            using (var response = await Get("api/client/" + _login)) // requests initial messages collection for current login
                _messages = (await response.Content.ReadAsAsync<IEnumerable<Message>>()).ToList();

            while (!token.IsCancellationRequested) Work();
        }

        private void Work()
        {
            Thread.Sleep(_random.Next(1, Interval) * 1000); // imitates client's activity time interval 

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
        }

        private void Update()
        {
            var copy = new Message[_messages.Count];
            _messages.CopyTo(copy);

            Message fresh;
            int i, l;

            for (i = 0, l = copy.Length; i < l; i++)
            {
                Task.Run(async () =>
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
                }).Wait();
            }
        }

        private void New()
        {
            var message = new Message
            {
                Client = _login,
                Contents = DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss ") + "test message from " + _login
            };

            Task.Run(async () =>
            {
                var response = await Post("api/client", message);
                message = await response.Content.ReadAsAsync<Message>();
                _messages.Add(message);
                WriteInline(_login + ": message created id: " + message.Id);
            }).Wait();
        }

        private void Cancel()
        {
            //throw new NotImplementedException();
        }

        public void Stop()
        {
            _messages.Clear();
        }

        
    }
}
