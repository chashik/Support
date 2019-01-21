using Support;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    public class User : ApiClient
    {
        private readonly List<Message> _messages;

        public User(CancellationToken token, string apiHost) : base(apiHost)
        {
         
        }

        public string Login { get; set; }

        public void Start()
        {

        }

        public void Stop()
        {

        }
    }
}
