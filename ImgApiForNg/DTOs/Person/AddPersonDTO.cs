using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ImgApiForNg.DTOs.Person
{
    public class AddPersonDTO
    {
        [Required]
        public string name { get; set; }
        [Required]
        public string city { get; set; }
        [Required]
        public IFormFile file { get; set; }
    }
}
