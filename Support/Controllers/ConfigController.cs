using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Support.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        private readonly IConfiguration _conf;


        public ConfigController(IConfiguration configuration)
        {
            _conf = configuration;
        }

        // GET: api/Config
        [HttpGet]
        public async Task<MyConfig> GetConfig() => await Task.Run(() => new MyConfig(_conf));
    }

    public class MyConfig
    {
        public MyConfig(IConfiguration configuration)
        {
            Tm = configuration.GetValue<int>("Tm");
            Td = configuration.GetValue<int>("Td");
            Tmin = configuration.GetValue<int>("Tmin");
            Tmax = configuration.GetValue<int>("Tmax");
        }

        public int Tm { get; private set; }
        public int Td { get; private set; }
        public int Tmin { get; private set; }
        public int Tmax { get; private set; }
    }
}