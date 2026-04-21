using Microsoft.EntityFrameworkCore;

namespace YourAPP_Persistence.Data.DbContext
{
    public class YourAPPDbContext(DbContextOptions<YourAPPDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
    {

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Additional model configurations can be added here
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(YourAPPDbContext).Assembly);

        }

    }
}
