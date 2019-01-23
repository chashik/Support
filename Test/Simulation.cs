using Microsoft.Extensions.Configuration;
using Support;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    internal class Simulation : ApiClient
    {
        private readonly MyConfig _myConfig;

        private IEnumerable<string> _operators;
        private IEnumerable<string> _managers;
        private IEnumerable<string> _directors;

        private ConcurrentQueue<Message> _messages;

        public Simulation(MyConfig conf)
        {
            _myConfig = conf;
            ApiHost = conf.ApiHost;

            Task.WaitAll(Employees(), Messages());
        }

        public string[] About
        {
            get
            {
                return new string[]
                {
                    "* Using ApiHost: " + ApiHost,
                    "* User emulators count: " + _myConfig.Users,

                    $"* Operators ({_operators.Count()}): " + string.Join(", ", _operators),
                    $"* Managers ({_managers.Count()}): " + string.Join(", ", _managers),
                    $"* Directors ({_directors.Count()}): " + string.Join(", ", _directors),
                    "* Minimal interval for client activity, sec (T): " + _myConfig.T,
                    "* Maximal interval for client activity, sec (Tc): " + _myConfig.Tc,
                    "* Minimal message age for managers, sec (Tm): " + _myConfig.Tm,
                    "* Minimal message age for directors, sec (Td): " + _myConfig.Td,
                    "* Minimal time per message for employee, sec (Tmin): " + _myConfig.Tmin,
                    "* Maximal time per message for employee, sec (Tmax): " + _myConfig.Tmax,
                    "* Awaiting messages in queue: " + _messages.Count
                };
            }
        }

        private async Task Employees()
        {
            using (var response = await Get("api/employees"))
            {
                var employees = await response.Content.ReadAsAsync<IEnumerable<Employee>>();

                var t1 = Task.Run(() => _operators = employees.Where(p => p.ManagerId != null).Select(p => p.Login));
                var t2 = Task.Run(() => _managers = employees.Where(p => p.DirectorId != null).Select(p => p.Login));
                var t3 = Task.Run(() => _directors = employees.Where(p => p.ManagerId == null && p.DirectorId == null)
                    .Select(p => p.Login));

                Task.WaitAll(t1, t2, t3);
            }
        }

        private async Task Messages()
        {
            using (var response = await Get("api/operator"))
                _messages = new ConcurrentQueue<Message>(await response.Content.ReadAsAsync<IEnumerable<Message>>());
        }

        public void Start()
        {
            Console.WriteLine("To stop simulation, press 's'");
            WriteInline("Getting things ready..");

            using (var tokenSource = new CancellationTokenSource())
            {
                var starts = new ConcurrentBag<Task>();
                var stops = new ConcurrentBag<Task>();
                var range = new int[_myConfig.Users];

                for (var i = 0; i < _myConfig.Users; i++) range[i] = i;

                range.AsParallel().ForAll(p => 
                {
                    var user = new User("client" + p) { Interval = _myConfig.Tc, ApiHost = ApiHost };
                    starts.Add(user.Start(tokenSource.Token));
                    stops.Add(new Task(() => user.Stop()));
                });

                char ch = Console.ReadKey().KeyChar;
                if (ch == 's' || ch == 'S')
                {
                    tokenSource.Cancel();
                    Console.WriteLine($"\nStop requested, please wait for {_myConfig.Tc} (Tc) sec.");
                    stops.AsParallel().ForAll(p => p.Start());
                    Task.WaitAll(stops.ToArray());
                }

                try { Task.WaitAll(starts.ToArray()); }
                catch (AggregateException ex)
                {
                    
                }
                finally
                {
                    Console.WriteLine();
                }
            }
        }
    }
}