using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ImgApiForNg.DTOs.Item
{
    public class ItemDTO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string filename { get; set; }
        public string filetype { get; set; }
        public string filesize { get; set; }
        public string filestring { get; set; }
    }
}
