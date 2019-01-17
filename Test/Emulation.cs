using Microsoft.Extensions.Configuration;

namespace Test
{
    internal class Emulation
    {
        private IConfigurationRoot conf;

        public Emulation(IConfigurationRoot conf)
        {
            this.conf = conf;
        }
    }
}