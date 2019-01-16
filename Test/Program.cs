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
                        break;
                    case "info":
                        break;
                    case "clear":
                        break;
                    default:
                        ArgumentsError();
                        return;
                }
            else ArgumentsError();

            Fin:
            Console.WriteLine("Press Enter to exit..");
            Console.ReadLine();
        }

        private static void ArgumentsError()
        {
            Console.WriteLine("Check arguments!");
            Console.WriteLine("Valid options are: emulate, info, clear.");
            Console.WriteLine("  Emulate - perform emulation;");
            Console.WriteLine("  Info - show emulation parameters, employees and messages queue statistics;");
            Console.WriteLine("  Clear - clear messages queue.");
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
    }
}
