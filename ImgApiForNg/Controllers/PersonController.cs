using ImgApiForNg.Data;
using ImgApiForNg.DTOs.Person;
using ImgApiForNg.Interfaces;
using ImgApiForNg.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImgApiForNg.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPersonRepository _personRepository;

        public PersonController(IPersonRepository personRepository ,ApplicationDbContext context)
        {
            _context = context;
            _personRepository = personRepository;
        }

        // GET: api/Person
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Person>>> GetPersons()
        {
            if (_context.Persons == null)
            {
                return NotFound();
            }
            return await _context.Persons.ToListAsync();
        }



        // POST: api/Person
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<AddPersonDTO>> PostPerson([FromForm]AddPersonDTO addPersonDTO)       //Code is not working
        {
            var locatedID = await _personRepository.Save(addPersonDTO);

            if (locatedID != 0)
            {
                return CreatedAtAction("GetPerson", new { id = locatedID }, addPersonDTO);
            }
            else
            {
                //return Problem("Entity set 'ApplicationDbContext.Persons'  is null.");
                return Problem("Entity set 'ApplicationDbContext.Persons'  is null.");
            }
        }



        // GET: api/Person/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Person>> GetPerson(int id)
        {
            if (_context.Persons == null)
            {
                return NotFound();
            }
            var person = await _context.Persons.FindAsync(id);

            if (person == null)
            {
                return NotFound();
            }

            return person;
        }

        // PUT: api/Person/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmployee(int id, Person person)
        {
            if (id != person.id)
            {
                return BadRequest();
            }

            _context.Entry(person).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PersonExists(id))
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

        

        // DELETE: api/Person/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePerson(int id)
        {
            if (_context.Persons == null)
            {
                return NotFound();
            }
            var person = await _context.Persons.FindAsync(id);
            if (person == null)
            {
                return NotFound();
            }

            _context.Persons.Remove(person);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PersonExists(int id)
        {
            return (_context.Persons?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}
