using ImgApiForNg.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ImgApiForNg.Repositories.Person
{
    public class PersonRepository
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




    }
}
