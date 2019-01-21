using Support;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    internal class Emulation : ApiClient
    {
        private readonly int _users;

        private MyConfig _myConfig;

        private IEnumerable<string> _operators;
        private IEnumerable<string> _managers;
        private IEnumerable<string> _directors;

        private CancellationTokenSource _tokenSource;

        private ConcurrentQueue<Message> _messages;

        public Emulation(string apiHost, int users) : base(apiHost)
        {
            _users = users;

            Task.WaitAll(ApiConf(), Employees(), Messages());

            /*using (_tokenSource = new CancellationTokenSource())
            {
                //_token = tokenSource.Token;
                Console.WriteLine("Press Ctrl+C to stop emulation.");
                Console.CancelKeyPress += Console_CancelKeyPress;

                DoSomework();
            }*/
        }

        private void DoSomework()
        {
            Thread.Sleep(5000);
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _tokenSource.Cancel();

            Console.WriteLine("Finishing emulation, please wait..");
            
            for (var i = 0; i < 6; i++)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(i);
                Thread.Sleep(1000);
            }

            Thread.CurrentThread.Join();
        }

        public void Information()
        {
            Console.WriteLine("* Using ApiHost: " + _apiHost);
            Console.WriteLine("* User emulators count: " + _users.ToString());
            Console.WriteLine($"* Operators ({_operators.Count()}): " + string.Join(", ", _operators));
            Console.WriteLine($"* Managers ({_managers.Count()}): " + string.Join(", ", _managers));
            Console.WriteLine($"* Directors ({_directors.Count()}): " + string.Join(", ", _directors));
            Console.WriteLine("* Minimal message age for managers, sec (Tm): " + _myConfig.Tm);
            Console.WriteLine("* Minimal message age for directors, sec (Td): " + _myConfig.Td);
            Console.WriteLine("* Minimal time per message for employee, sec (Tmin): " + _myConfig.Tmin);
            Console.WriteLine("* Maximal time per message for employee, sec (Tmax): " + _myConfig.Tmax);

            var waiting = _messages.Count;
            Console.WriteLine("* Awaiting messages in queue: " + waiting);
            if (waiting > 0)
                Console.WriteLine("* Oldest message from: " + _messages.First().Created.ToString("YYYY/MM/DD hh:mm:ss"));
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

        private async Task ApiConf()
        {
            using (var response = await Get("api/config"))
                _myConfig = await response.Content.ReadAsAsync<MyConfig>();
        }
    }
}