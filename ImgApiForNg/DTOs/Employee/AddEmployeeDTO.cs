using Microsoft.AspNetCore.Http;

namespace ImgApiForNg.DTOs.Employee
{
    public class AddEmployeeDTO
    {
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string birthdate { get; set; }
        public string gender { get; set; }
        public string education { get; set; }
        public string company { get; set; }
        public string jobExperience { get; set; }
        public string salary { get; set; }
        public IFormFile? profile { get; set; }
    }
}
