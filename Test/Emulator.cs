using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using Support;

namespace Test
{
    internal abstract class Emulator
    {
        private readonly HttpClient _httpClient;
        private readonly string _login;

        public Emulator(string login)
        {

        }
    }
}
