using Microsoft.Extensions.Configuration;
using Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Test
{
    internal class Information
    {
        private readonly string _apiHost;
        private readonly int _clients;

        public Information(IConfigurationRoot conf)
        {
            _apiHost = conf.GetValue<string>("ApiHost");
            _clients = conf.GetValue<int>("Clients");

            Task.WaitAll(Employees());
        }

        public void Output()
        {
            Console.WriteLine("* Using ApiHost: " + _apiHost);
            Console.WriteLine("* Client emulators count: " + _clients.ToString());
            Console.WriteLine($"* Operators ({_operators.Length}): " + string.Join(", ", _operators));
            Console.WriteLine($"* Managers ({_managers.Length}): " + string.Join(", ", _managers));
            Console.WriteLine($"* Directors ({_directors.Length}): " + string.Join(", ", _directors));
        }

        private async Task Employees()
        {
            using (var response = await HttpHelp.Get(_apiHost, "api/employees"))
            {
                var employees = await response.Content.ReadAsAsync<IEnumerable<Employee>>();

                var t1 = Task.Run(() => _operators = employees.Where(p => p.ManagerId != null).Select(p => p.Login).ToArray());
                var t2 = Task.Run(() => _managers = employees.Where(p => p.DirectorId != null).Select(p => p.Login).ToArray());
                var t3 = Task.Run(() => _directors = employees.Where(p => p.ManagerId == null && p.DirectorId == null)
                    .Select(p => p.Login).ToArray());

                Task.WaitAll(t1, t2, t3);
            }
        }

        private async Task ApiConf()
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiHost);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await httpClient.GetAsync("api/employees");
            }
        }

        private string[] _operators;
        private string[] _managers;
        private string[] _directors;

        private int _tm;
        private int _td;
        private int _tmin;
        private int _tmax;

        private int _waiting;
    }
}