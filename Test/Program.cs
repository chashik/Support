using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var conf = Configuration();
                if (conf != null)
                    Start(conf, args[0].Trim('-').ToLower());
            }
            else
                ArgumentsError();

            Console.WriteLine("Press Enter to exit..");
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

        private static void Start(IConfigurationRoot conf, string arg)
        {
            var apiHost = conf.GetValue<string>("ApiHost");
            var users = conf.GetValue<int>("Users");
            switch (arg)
            {
                case "emulate":
                    Emulate(apiHost, users);
                    break;
                case "info":
                    Inform(apiHost, users);
                    break;
                case "clear":
                    Clear(apiHost);
                    break;
                default:
                    ArgumentsError();
                    break;
            }
        }

        private static void Emulate(string apiHost, int users)
        {
            var emu = new Emulation(apiHost, users);
        }

        private static void Inform(string apiHost, int users)
        {
            var emu = new Emulation(apiHost, users);
            var info = emu.Information;
            foreach (var str in info) Console.WriteLine(str);
        }

        private static void Clear(string apiHost)
        {
            var cleaner = new Cleaner(apiHost);
        }

        private static void ArgumentsError()
        {
            Console.WriteLine("Check arguments!");
            Console.WriteLine("Valid options are: emulate, info, clear.");
            Console.WriteLine("  Emulate - perform emulation;");
            Console.WriteLine("  Info - show emulation parameters, employees and messages queue statistics;");
            Console.WriteLine("  Clear - clear messages queue.");
        }
    }
}
