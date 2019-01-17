using Microsoft.Extensions.Configuration;

namespace Test
{
    internal class Cleaner
    {
        private IConfigurationRoot conf;

        public Cleaner(IConfigurationRoot conf)
        {
            this.conf = conf;
        }
    }
}