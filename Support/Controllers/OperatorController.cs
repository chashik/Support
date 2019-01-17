using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Support.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OperatorController : ControllerBase
    {
        private readonly SupportContext _context;

        public OperatorController(SupportContext context)
        {
            _context = context;
        }

        // GET: api/Operator
        [HttpGet]
        public IEnumerable<Message> GetMessages() => _context.Messages.Where(p => !p.Cancelled && p.OperatorId == null);

        // GET: api/Operator/2?login=operator1
        [HttpGet("{offset:int}")]
        public async Task<IActionResult> GetMessage(
            [FromQuery] string login, 
            [FromRoute] int offset)
        {
            var created = DateTime.Now.AddSeconds(-offset);

            try
            {
                var message = await _context.Messages
                  .Where(p => p.Created < created && !p.Cancelled && p.OperatorId == null)
                  .OrderBy(p => p.Id)
                  .FirstAsync();

                message.OperatorId = login;

                _context.Entry(message).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                return Ok(message);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        // PUT: api/Operator/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMessage([FromRoute] int id, [FromBody] Message message)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != message.Id)
            {
                return BadRequest();
            }

            _context.Entry(message).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MessageExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private bool MessageExists(int id) => _context.Messages.Any(e => e.Id == id);
    }
}