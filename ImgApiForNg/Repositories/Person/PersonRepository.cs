using ImgApiForNg.Data;
using ImgApiForNg.Models;
using ImgApiForNg.DTOs.Person;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using ImgApiForNg.Interfaces;

namespace ImgApiForNg.Repositories
{
    public class PersonRepository: IPersonRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public PersonRepository(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)//, IConfiguration configuration)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            //_configuration = configuration;
        }




        public async Task<int> Save(AddPersonDTO addPersonDTO)
        {

            if (addPersonDTO.file.Length > 0)
            {
                Person person = new Person()
                {
                    name = addPersonDTO.name,
                    city = addPersonDTO.city,
                    filename = addPersonDTO.file.FileName,
                    filebytes = IFormFileToBytesArray(addPersonDTO.file),
                    apiurl = "Not Implemented",
                    clienturl = "Not Implemented"
                };


                try
                {
                    _context.Persons.Add(person);
                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    return 0;
                }
                return person.id;
            }


            return 0;
        }














        public static byte[] IFormFileToBytesArray(IFormFile ImageIFormFile)
        {
            var ms = new MemoryStream();
            ImageIFormFile.CopyTo(ms);
            var BytesPhoto = ms.ToArray();

            return BytesPhoto;
        }


        public static IFormFile BytesArrayToIFormFile(byte[] BytesPhoto, string filename = "FileName")
        {
            var stream = new MemoryStream(BytesPhoto);
            IFormFile ImageIFormFile = new FormFile(stream, 0, (BytesPhoto).Length, "name", filename);
            return ImageIFormFile;
        }
    }
}
