using Microsoft.Extensions.Configuration;
using Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Test
{
    internal class Information
    {
        private readonly string _apiHost;
        private readonly int _clients;

        private string[] _operators;
        private string[] _managers;
        private string[] _directors;

        private MyConfig _myConfig;

        private int _waiting;
        private string _oldest;

        public Information(IConfigurationRoot conf)
        {
            _apiHost = conf.GetValue<string>("ApiHost");
            _clients = conf.GetValue<int>("Clients");

            Task.WaitAll(Employees(), ApiConf(), Messages());
        }

        public void Output()
        {
            Console.WriteLine("* Using ApiHost: " + _apiHost);
            Console.WriteLine("* Client emulators count: " + _clients.ToString());
            Console.WriteLine($"* Operators ({_operators.Length}): " + string.Join(", ", _operators));
            Console.WriteLine($"* Managers ({_managers.Length}): " + string.Join(", ", _managers));
            Console.WriteLine($"* Directors ({_directors.Length}): " + string.Join(", ", _directors));
            Console.WriteLine("* Minimal message age for managers, sec (Tm): " + _myConfig.Tm);
            Console.WriteLine("* Minimal message age for directors, sec (Tm): " + _myConfig.Td);
            Console.WriteLine("* Minimal time per message for employee, sec (Tmin): " + _myConfig.Tmin);
            Console.WriteLine("* Maximal time per message for employee, sec (Tmax): " + _myConfig.Tmax);
            Console.WriteLine("* Awaiting messages in queue: " + _waiting);
            if (_oldest != null) Console.WriteLine("* Oldest message from: " + _oldest);
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
            using (var response = await HttpHelp.Get(_apiHost, "api/config"))
                _myConfig = await response.Content.ReadAsAsync<MyConfig>();
        }

        private async Task Messages()
        {
            using (var response = await HttpHelp.Get(_apiHost, "api/operator"))
            {
                var messages = await response.Content.ReadAsAsync<IEnumerable<Message>>();
                _waiting = messages.Count();
                if (_waiting > 0)
                    _oldest = messages.OrderBy(p => p.Created).First().Created.ToString("YYYY/MM/DD hh:mm:ss");
            }
        }
    }
}