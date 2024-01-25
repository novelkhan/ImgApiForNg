using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ImgApiForNg.Models
{
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string birthdate { get; set; }
        public string gender { get; set; }
        public string education { get; set; }
        public string company { get; set; }
        public int jobExperience { get; set; }
        public int salary { get; set; }
        public string profile { get; set; }
    }
}
