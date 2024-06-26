﻿using ImgApiForNg.Data;
using ImgApiForNg.DTOs.Person;
using ImgApiForNg.Interfaces;
using ImgApiForNg.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
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




        [HttpGet("file/{id}")]
        public async Task<ActionResult> GetFileAsync(int id)
        {
            var person = await _context.Persons.Where(n => n.id == id).FirstOrDefaultAsync();
            
            if (person != null)
            {
                //var contentType = "APPLICATION/octet-stream";
                //MemoryStream memoryStream = BytesArrayToIFormFileMemoryStream(person.filebytes, person.filename);
                var memoryStream = new MemoryStream(person.filebytes);
                memoryStream.Position = 0;
                return File(memoryStream, person.filetype, person.filename);
            }

            return NotFound();
        }



        // PUT: api/Person/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPerson(int id, Person person)
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

        public static MemoryStream BytesArrayToIFormFileMemoryStream(byte[] BytesPhoto, string filename = "FileName")
        {
            var stream = new MemoryStream(BytesPhoto);
            IFormFile ImageIFormFile = new FormFile(stream, 0, (BytesPhoto).Length, "name", filename);
            var memoryStream = new MemoryStream();
            ImageIFormFile.CopyTo(memoryStream);
            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}
