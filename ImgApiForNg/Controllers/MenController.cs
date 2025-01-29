using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ImgApiForNg.Data;
using ImgApiForNg.Models;
using System.IO;

namespace ImgApiForNg.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MenController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Men
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Man>>> GetMen()
        {
          if (_context.Men == null)
          {
              return NotFound();
          }
            return await _context.Men.ToListAsync();
        }

        // GET: api/Men/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Man>> GetMan(int id)
        {
          if (_context.Men == null)
          {
              return NotFound();
          }
            var man = await _context.Men.FindAsync(id);

            if (man == null)
            {
                return NotFound();
            }

            return man;
        }

        // PUT: api/Men/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMan(int id, Man man)
        {
            if (id != man.id)
            {
                return BadRequest();
            }

            _context.Entry(man).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ManExists(id))
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

        // POST: api/Men
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Man>> PostMan([FromBody]Man man)
        {
            if (_context.Men == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Men'  is null.");
            }

            var mapedMan = new Man()
            {
                name = man.name,
                filename = man.filename,
                filetype = man.filetype,
                filesize = man.filesize,
                imagebytes = Base64StringToBytesArray(man.base64string),
                base64string = man.base64string
            };

            _context.Men.Add(mapedMan);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMan", new { id = mapedMan.id }, mapedMan);
        }

        // DELETE: api/Men/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMan(int id)
        {
            if (_context.Men == null)
            {
                return NotFound();
            }
            var man = await _context.Men.FindAsync(id);
            if (man == null)
            {
                return NotFound();
            }

            _context.Men.Remove(man);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ManExists(int id)
        {
            return (_context.Men?.Any(e => e.id == id)).GetValueOrDefault();
        }




        public static byte[] Base64StringToBytesArray(String base64String)
        {
            var fileBytes = Convert.FromBase64String(base64String);

            return fileBytes;
        }
        
        public static string BytesArrayToBase64String(byte[] bytesArray)
        {
            // Byte array (bytesArray) থেকে Base64 String তৈরি
            string base64String = Convert.ToBase64String(bytesArray);

            return base64String;
        }
    }
}
