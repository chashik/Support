using System.Net.Http;

namespace Test
{
    internal abstract class Emulator
    {
        protected readonly HttpClient _httpClient;
        protected readonly string _login;

        public Emulator(string login)
        {
            _login = login;
            _httpClient = new HttpClient();
        }

        internal string ApiHost { get; set; }
    }
}
