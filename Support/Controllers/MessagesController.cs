using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Support.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class MessagesController : ControllerBase
    {
        private readonly SupportContext _context;

        public MessagesController(SupportContext context) =>
            _context = context;


        // GET: api/Messages - all unanswered messages
        [HttpGet]
        public async Task<IActionResult> GetMessages() =>
            await Task.Run<IActionResult>(() => Ok(_context.Messages
                .Where(p => p.Finished == null).Select(p => p.ShallowCopy()).ToArray()));


        // GET: api/Messages/login - unanswered messages for client's login
        [HttpGet("{login}")]
        public async Task<IActionResult> GetMessages([FromRoute] string login) =>
            await Task.Run<IActionResult>(() => Ok(_context.Messages
                .Where(p => p.Finished == null && p.Client == login)
                .Select(p => p.ShallowCopy()).ToArray()));


        // GET: api/Messages/login/num - common message selector 
        // for clients and employees by login, id/time offset
        [HttpGet("{login}/{num:int}")]
        public async Task<IActionResult> GetMessage([FromRoute] string login, [FromRoute] int num)
        {
            Message message = null; 

            if (await _context.Employees.AnyAsync(p => p.Login == login)) // num as time offset for employee
            {
                var messages = _context.Messages // unfinished messages for current employee first
                    .Where(p => p.OperatorId == login && p.Finished == null)
                    .ToArray();

                if (messages.Length > 0)
                    message = messages.OrderBy(p => p.Id).First();
                else
                {
                    var created = DateTime.Now.AddSeconds(-num);
                    messages = _context.Messages // messages suitable with time offset
                        .Where(p => p.OperatorId == null && p.Finished == null && p.Created < created)
                        .ToArray();

                    if (messages.Length > 0)
                        message = messages.OrderBy(p => p.Id).First();
                }
            }
            else // num as id for client
                message = await _context.Messages.FindAsync(num);

            if (message == null)
                return NotFound();
            else
                return Ok(message);
        }

        // PUT: api/Messages/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMessage([FromRoute] int id, [FromBody] Message message)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != message.Id)
                return BadRequest();

            if (message.Cancelled || message.Answer != null)
                message.Finished = DateTime.Now;

            _context.Entry(message).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MessageExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // POST: api/Messages
        [HttpPost]
        public async Task<IActionResult> PostMessage([FromBody] Message message)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            message.Created = DateTime.Now;
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMessage",
                new { login = message.Client, num = message.Id }, message);
        }

        // DELETE: api/Messages
        [HttpDelete]
        public async Task<IActionResult> DeleteMessages() =>
            await Task.Run<IActionResult>(() =>
            {
                try
                {
                    var c = _context.Database
                        .ExecuteSqlCommand("DELETE FROM [support].[dbo].[message]");
                    var r = _context.Database
                        .ExecuteSqlCommand("DBCC CHECKIDENT ('[support].[dbo].[message]', RESEED, 0)");
                    return Ok(c);
                }
                catch (Exception ex)
                {
                    return NotFound(ex.Message);
                }
            });

        // DELETE: api/Messages/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage([FromRoute] int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var message = await _context.Messages.FindAsync(id);
            if (message == null)
                return NotFound();

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return Ok(message);
        }

        private bool MessageExists(int id) => 
            _context.Messages.Any(e => e.Id == id);
    }
}