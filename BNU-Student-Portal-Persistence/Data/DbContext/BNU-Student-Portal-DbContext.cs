using BNU_Student_Portal_Domain.Entities.Auth;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BNU_Student_Portal_Persistence.Data.DbContext
{
    public class BNU_Student_Portal_DbContext(DbContextOptions<BNU_Student_Portal_DbContext> options) : IdentityDbContext<AppUser>(options)
    {

        public DbSet<Student> Students { get; set; }
        public DbSet<Professor> Professors { get; set; }
        public DbSet<TeachingAssistant> TeachingAssistants { get; set; }
        public DbSet<Guardian> Guardians { get; set; }
        public DbSet<Address> Addresses { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Additional model configurations can be added here
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BNU_Student_Portal_DbContext).Assembly);

        }
    }
}
