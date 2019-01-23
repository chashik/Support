using Microsoft.Extensions.Configuration;

namespace Test
{
    public class MyConfig
    {
        public MyConfig(IConfiguration configuration)
        {
            ApiHost = configuration.GetValue<string>("ApiHost");
            Users = configuration.GetValue<int>("Users");
            T = configuration.GetValue<int>("T");
            Tc = configuration.GetValue<int>("Tc");
            Tm = configuration.GetValue<int>("Tm");
            Td = configuration.GetValue<int>("Td");
            Tmin = configuration.GetValue<int>("Tmin");
            Tmax = configuration.GetValue<int>("Tmax");
        }

        public readonly string ApiHost;
        public readonly int Users;
        public readonly int T;
        public readonly int Tc;
        public readonly int Tm;
        public readonly int Td;
        public readonly int Tmin;
        public readonly int Tmax;
    }
}
