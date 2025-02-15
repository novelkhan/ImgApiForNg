using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace ImgApiForNg.Models
{
    public class Prop
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        public string fileName { get; set; }
        public string fileType { get; set; }
        public string fileSize { get; set; }
        public string fileUrl { get; set; }

        // Add these two properties for download link functionality
        public string DownloadToken { get; set; } = string.Empty; // Default to empty string
        public DateTime? DownloadTokenExpiration { get; set; } = null; // Default to null
    }
}
