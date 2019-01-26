using Support;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        private IEnumerable<Message> _messages;

        public Simulation(MyConfig conf)
        {
            _myConfig = conf;
            ApiHost = conf.ApiHost;

            Task.WaitAll(Employees(), Messages());
        }

        public Task<string[]> About
        {
            get
            {
                return Task.Run(() => new string[]
                {
                    $"* Using ApiHost: {ApiHost}",
                    $"* User emulators count: {_myConfig.Users}",

                    $"* Operators ({_operators.Count()}): {string.Join(", ", _operators)}",
                    $"* Managers ({_managers.Count()}): {string.Join(", ", _managers)}",
                    $"* Directors ({_directors.Count()}): {string.Join(", ", _directors)}",
                    $"* Minimal interval for client activity, sec (T): {_myConfig.T}",
                    $"* Maximal interval for client activity, sec (Tc): {_myConfig.Tc}",
                    $"* Minimal message age for managers, sec (Tm): {_myConfig.Tm}",
                    $"* Minimal message age for directors, sec (Td): {_myConfig.Td}",
                    $"* Minimal time per message for employee, sec (Tmin): {_myConfig.Tmin}",
                    $"* Maximal time per message for employee, sec (Tmax): {_myConfig.Tmax}",
                    $"* Awaiting messages in queue: {_messages.Count()}"
                });
            }
        }

        private async Task Employees()
        {
            await Task.Run(() =>
            {
                if (Get("api/employees", out HttpStatusCode code, out IEnumerable<Employee> employees))
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
                    WriteInline($"Loading employees failed: {code}");
            });
        }

        private async Task Messages()
        {
            await Task.Run(() =>
            {
                if (Get("api/client", out HttpStatusCode code, out _messages))
                {

                }
                else
                    WriteInline($"Loading unanswered messages failed: {code}");
            });
        }

        public async Task Start()
        {
            Console.WriteLine("To stop simulation, press 's'");
            WriteInline("Getting things ready, please wait..");

            var range = new int[_myConfig.Users];
            int i, l;
            for (i = 0, l = _myConfig.Users; i < l; i++) range[i] = i;

            using (var source = new CancellationTokenSource())
            {
                await Task.Run(() =>
                {
                    var token = source.Token;
                    var starts = new ConcurrentBag<Task>();
                    var simulators = new ConcurrentBag<ISimulator>();

                    var t1 = Task.Run(() => range.AsParallel().ForAll(p =>
                         {
                             var user = new User("client" + p)
                             {
                                 T = _myConfig.T,
                                 Tc = _myConfig.Tc,
                                 ApiHost = ApiHost
                             };
                             starts.Add(new Task(() => user.Start(token)));
                             simulators.Add(user);
                         }));

                    void createOperator(string login, int offset)
                    {
                        var employee = new Operator(login)
                        {
                            Offset = offset,
                            Tmin = _myConfig.Tmin,
                            Tmax = _myConfig.Tmax,
                            ApiHost = ApiHost
                        };
                        starts.Add(new Task(() => employee.Start(token)));
                        simulators.Add(employee);
                    }

                    var t2 = Task.Run(() =>
                      {
                          foreach (var o in _operators)
                              createOperator(o, 0);
                          foreach (var m in _managers)
                              createOperator(m, _myConfig.Tm);
                          foreach (var d in _directors)
                              createOperator(d, _myConfig.Td);
                      });

                    Task.WaitAll(t1, t2);

                    starts.AsParallel().ForAll(p => p.Start());

                    var waitingInput = true;

                    while (waitingInput)
                    {
                        char ch = Console.ReadKey().KeyChar;
                        if (ch == 's' || ch == 'S' || ch == 'ы' || ch == 'Ы')
                        {
                            Console.WriteLine($"\nStop requested... ");
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

                    Console.WriteLine("\rFinishing tasks, wait..");

                    foreach (var u in simulators) // waiting while child tasks are not finished
                        while (u.Pool.Count > 0) Thread.Sleep(1000);
                });
            }
        }
    }
}