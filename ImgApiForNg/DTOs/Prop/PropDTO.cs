using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ImgApiForNg.DTOs.Prop
{
    public class PropDTO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string filename { get; set; }
        public string filetype { get; set; }
        public string filesize { get; set; }
        public List<byte> filedata { get; set; }
    }
}
