using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ImgApiForNg.DTOs.Item
{
    public class ItemDTO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string fileName { get; set; }
        public string fileType { get; set; }
        public string fileSize { get; set; }
        public byte[] file { get; set; }
    }
}
