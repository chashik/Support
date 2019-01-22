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
        private readonly CancellationToken _token;
        private readonly Random _random;

        private List<Message> _messages;
        private int _offset;
        private string _login;

        public User(CancellationToken token, string apiHost) : base(apiHost)
        {
            _token = token;
            _random = new Random();
        }

        public void Start(string login, int offset)
        {
            _login = login;
            _offset = offset;

            Task.Run(async () =>
            {
                using (var response = await Get("api/client"))
                    _messages = (await response.Content.ReadAsAsync<IEnumerable<Message>>()).ToList();
            }).Wait(); // requests initial messages collection for current login

            while (!_token.IsCancellationRequested) Work();
        }

        private void Work()
        {
            Thread.Sleep(_random.Next(1, _offset) * 1000); // imitates client's activity time interval 

            Update(); // checks status and updates messages list for current login

            if (_messages.Count > 0) // "flips a coin" to choose whether to create new message or cancel an older one
                if (Convert.ToBoolean(_random.Next(0, 1)))
                    New();
                else
                    Cancel();
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
                    using (var response = await Get("api/client/" + old.Id))
                    {
                        fresh = await response.Content.ReadAsAsync<Message>();

                        if (fresh.Finished != null) _messages.Remove(old);
                        else _messages[_messages.IndexOf(old)] = fresh;
                    }
                }).Wait();
            }
        }

        private void New()
        {
            throw new NotImplementedException();
        }

        private void Cancel()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {

        }
    }
}
