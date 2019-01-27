using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
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
            switch (arg)
            {
                case "sim":
                    await Simulate(conf);
                    break;
                case "info":
                    await Inform(conf);
                    break;
                case "clear":
                    await Clear(conf.GetValue<string>("ApiHost"));
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
            foreach (var str in info)
                Console.WriteLine(str);
        }

        private static async Task Clear(string apiHost)
        {
            await Task.Run(() =>
            {
                if (ApiClient.Delete(apiHost, "api/messages", out HttpStatusCode code, out int count))
                    Console.WriteLine("Succesfully deleted {0} items and run reseed", count);
                else
                    Console.WriteLine("Unexpected result, HttpStatus: {}", code);
            });
        }

        private static async Task ArgumentsError()
        {
            await Task.Run(() =>
            {
                var output = new string[]
                {
                    "Check arguments!",
                    "Valid options are: sim, info, clear.",
                    "* Info - show parameters, employees and messages queue statistics;",
                    "* Sim - start simulation;",
                    "* Clear - clear server messages queue and reset Id;",
                    "Try to check params ('info') first."
                };

                foreach (var s in output)
                    Console.WriteLine(s);
            });
        }
    }
}
