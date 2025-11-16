using Microsoft.EntityFrameworkCore;

namespace Hospital.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // 后面表都在这里声明，例如：
        // public DbSet<Patient> Patients { get; set; }
    }
}
