using ImgApiForNg.Models;
using Microsoft.EntityFrameworkCore;

namespace ImgApiForNg.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
    }
}
