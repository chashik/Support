using Microsoft.Extensions.Configuration;

namespace Test
{
    internal class Cleaner
    {
        private readonly string _apiHost;

        public Cleaner(string apiHost)
        {
            _apiHost = apiHost;
        }
    }
}