using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var conf = Configuration();

            if (conf == null) goto Fin;

            if (args.Length > 0)
                switch (args[0].Trim('-').ToLower())
                {
                    case "emulate":
                        Emulate(conf);
                        break;
                    case "info":
                        Inform(conf);
                        break;
                    case "clear":
                        Clear(conf);
                        break;
                    default:
                        ArgumentsError();
                        break;
                }
            else
                ArgumentsError();

            Fin:

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

        private static void Emulate(IConfigurationRoot conf)
        {
            var emu = new Emulation(conf);
        }

        private static void Inform(IConfigurationRoot conf)
        {
            var info = new Information(conf);
            info.Output();
        }

        private static void Clear(IConfigurationRoot conf)
        {
            var cleaner = new Cleaner(conf);
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
