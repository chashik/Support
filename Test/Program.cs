using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0) // checks arguments count first
            {
                var arg0 = args[0].Trim('-').ToLower();
                var conf = Configuration();

                if (conf != null) // then configuration file
                    Start(arg0, conf);
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

        private static void Start(string arg, IConfigurationRoot conf)
        {
            var apiHost = conf.GetValue<string>("ApiHost");
            var users = conf.GetValue<int>("Users");
            switch (arg)
            {
                case "sim":
                    Simulate(conf);
                    break;
                case "info":
                    Inform(conf);
                    break;
                case "clear":
                    Clear(apiHost);
                    break;
                default:
                    ArgumentsError();
                    break;
            }
        }

        private static void Simulate(IConfigurationRoot conf)
        {
            var emu = new Simulation(new MyConfig(conf));
            emu.Start();
        }

        private static void Inform(IConfigurationRoot conf)
        {
            var emu = new Simulation(new MyConfig(conf));
            var info = emu.About;
            foreach (var str in info) Console.WriteLine(str);
        }

        private static void Clear(string apiHost)
        {
            var cleaner = new Cleaner(apiHost);
        }

        private static void ArgumentsError()
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
        }
    }
}
