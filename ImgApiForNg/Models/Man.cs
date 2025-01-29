using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ImgApiForNg.Models
{
    public class Man
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string name { get; set; }
        public string filename { get; set; }
        public string filetype { get; set; }
        public string filesize { get; set; }
        public byte[]? imagebytes { get; set; }
        public string base64string { get; set; }
    }
}
