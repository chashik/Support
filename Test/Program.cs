using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length > 0) // checks arguments count first
            {
                var arg0 = args[0].Trim('-').ToLower();
                var conf = Configuration();

                if (conf != null) // then configuration file
                    await Start(arg0, conf);
            }
            else
                await ArgumentsError();

            Console.WriteLine("\nPress Enter to exit..");
            Console.ReadLine();
            
            /*await Task.Run(() =>
            {
                var ago = DateTime.Now.AddSeconds(-1000);
                Console.WriteLine(DateTime.Now > ago); // true
                Console.ReadLine();
            });*/
        }

        private static IConfigurationRoot Configuration()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json");

                return builder.Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        private static async Task Start(string arg, IConfigurationRoot conf)
        {
            var apiHost = conf.GetValue<string>("ApiHost");
            var users = conf.GetValue<int>("Users");
            switch (arg)
            {
                case "sim":
                    await Simulate(conf);
                    break;
                case "info":
                    await Inform(conf);
                    break;
                case "clear":
                    await Clear(apiHost);
                    break;
                default:
                    await ArgumentsError();
                    break;
            }
        }

        private static async Task Simulate(IConfigurationRoot conf)
        {
            var emu = new Simulation(new MyConfig(conf));
            await emu.Start();
        }

        private static async Task Inform(IConfigurationRoot conf)
        {
            var emu = new Simulation(new MyConfig(conf));
            var info = await emu.About;
            foreach (var str in info) Console.WriteLine(str);
        }

        private static async Task Clear(string apiHost)
        {
            throw new NotImplementedException();
        }

        private static async Task ArgumentsError()
        {
            await Task.Run(() =>
            {
                var output = new string[]
                {
                "Check arguments!",
                "Valid options are: sim, info, clear.",
                "* Sim - start simulation;",
                "* Info - show parameters, employees and messages queue statistics;",
                "* Clear - clear server messages queue.",
                "Try to check params 'info' before simulation 'sim'."
                };

                foreach (var s in output) Console.WriteLine(s);
            });
        }
    }
}
