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
    internal class Simulation
    {
        private readonly MyConfig _myConfig;

        private IEnumerable<string> _operators;
        private IEnumerable<string> _managers;
        private IEnumerable<string> _directors;

        private IEnumerable<Message> _messages;

        public Simulation(MyConfig conf)
        {
            _myConfig = conf;

            Task.WaitAll(Employees(), Messages());
        }

        public Task<string[]> About
        {
            get
            {
                var mc = _messages.Count();
                var lastId = mc > 0 ? (int?)_messages.Select(p => p.Id).Max() : null;

                return Task.Run(() => new string[]
                {
                    $"* Using ApiHost: {_myConfig.ApiHost}",
                    $"* User simulators count: {_myConfig.Users}",

                    $"* Operators ({_operators.Count()}): {string.Join(", ", _operators)}",
                    $"* Managers ({_managers.Count()}): {string.Join(", ", _managers)}",
                    $"* Directors ({_directors.Count()}): {string.Join(", ", _directors)}",
                    $"* Minimal interval for client activity, sec (T): {_myConfig.T}",
                    $"* Maximal interval for client activity, sec (Tc): {_myConfig.Tc}",
                    $"* Minimal message age for managers, sec (Tm): {_myConfig.Tm}",
                    $"* Minimal message age for directors, sec (Td): {_myConfig.Td}",
                    $"* Minimal time per message for employee, sec (Tmin): {_myConfig.Tmin}",
                    $"* Maximal time per message for employee, sec (Tmax): {_myConfig.Tmax}",
                    $"* Awaiting messages in queue: {mc}",
                    $"* Last Id: {lastId}"
                });
            }
        }

        public bool Failure { get; private set; }

        private async Task Employees()
        {
            await Task.Run(() =>
            {
                if (ApiClient.Get(_myConfig.ApiHost, "api/employees",
                    out HttpStatusCode code, out IEnumerable<Support.Employee> employees))
                {
                    _operators = employees
                        .Where(p => p.ManagerId != null).Select(p => p.Login);
                    _managers = employees
                        .Where(p => p.DirectorId != null).Select(p => p.Login);
                    _directors = employees
                        .Where(p => p.ManagerId == null && p.DirectorId == null)
                        .Select(p => p.Login);
                }
                else
                {
                    Console.WriteLine($"Loading employees failed: {code}");
                    Failure = true;
                }
            });
        }

        private async Task Messages()
        {
            await Task.Run(() =>
            {
                if (!ApiClient.Get(_myConfig.ApiHost, "api/messages",
                    out HttpStatusCode code, out _messages))
                {
                    Console.WriteLine($"Loading unanswered messages failed: {code}");
                    Failure = true;
                }
            });
        }

        public async Task Start()
        {
            Console.WriteLine("Getting things ready, please wait..");

            var range = new int[_myConfig.Users];
            int i, l;
            for (i = 0, l = _myConfig.Users; i < l; i++)
                range[i] = i;

            using (var source = new CancellationTokenSource())
            {
                await Task.Run(() =>
                {
                    var token = source.Token;
                    var starts = new ConcurrentBag<Task>();
                    var simulators = new ConcurrentBag<ISimulator>();
                    var apiHost = _myConfig.ApiHost;

                    var t1 = Task.Run(() => range.AsParallel().ForAll(p =>
                    {
                        var user = new User(apiHost)
                        {
                            Tmin = _myConfig.T,
                            Tmax = _myConfig.Tc,
                            Login = "client" + p
                        };
                        starts.Add(new Task(() => user.Start(token)));
                        simulators.Add(user);
                    }));

                    void createEmployee(string login, int offset)
                    {
                        var employee = new Employee(apiHost)
                        {
                            Offset = offset,
                            Tmin = _myConfig.Tmin,
                            Tmax = _myConfig.Tmax,
                            Login = login
                        };
                        starts.Add(new Task(() => employee.Start(token)));
                        simulators.Add(employee);
                    }

                    var t2 = Task.Run(() =>
                    {
                        foreach (var o in _operators)
                            createEmployee(o, 0);
                        foreach (var m in _managers)
                            createEmployee(m, _myConfig.Tm);
                        foreach (var d in _directors)
                            createEmployee(d, _myConfig.Td);
                    });

                    Task.WaitAll(t1, t2);

                    Console.WriteLine("To stop simulation press 's'..");

                    starts.AsParallel().ForAll(p => p.Start());

                    var waitingInput = true;

                    while (waitingInput)
                    {
                        char ch = Console.ReadKey().KeyChar;
                        if (ch == 's' || ch == 'S' || ch == 'ы' || ch == 'Ы') // Ы :)
                        {
                            Console.WriteLine($"\nFinishing tasks, please wait.. ");
                            source.Cancel();
                            waitingInput = false;
                        }
                    }

                    try
                    {
                        Task.WaitAll(starts.ToArray());
                    }
                    catch (AggregateException ex)
                    {
                        foreach (var v in ex.InnerExceptions)
                            Console.WriteLine(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Other exception: {0}", ex.Message);
                    }
                });
            }
        }
    }
}
