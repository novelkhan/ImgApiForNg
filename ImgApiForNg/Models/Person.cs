using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ImgApiForNg.Models
{
    public class Person
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string name { get; set; }
        public string city { get; set; }
        public string filename { get; set; }
        public byte[]? filebytes { get; set; }
        public string? apiurl { get; set; }
        public string? clienturl { get; set; }
    }
}
