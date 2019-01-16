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
            if (conf == null) return;



            Console.WriteLine(conf.GetValue<string>("ApiHost"));

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
    }
}
