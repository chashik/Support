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
}