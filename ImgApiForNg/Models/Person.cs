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
        public string picturename { get; set; }
        public byte[] picturebytes { get; set; }
        public string? pictureurl { get; set; }
    }
}
