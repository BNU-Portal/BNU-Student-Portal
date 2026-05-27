// BNU-Student-Portal-Persistence/Data/DbContext/BNU_Student_Portal_DbContextFactory.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BNU_Student_Portal_Persistence.Data.DbContext;

public class BNU_Student_Portal_DbContextFactory
    : IDesignTimeDbContextFactory<BNU_Student_Portal_DbContext>
{
    public BNU_Student_Portal_DbContext CreateDbContext(string[] args)
    {
        // Walk up from Persistence project to find appsettings.json in the Web project
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(),
                "../BNU-Student-Portal-Web"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<BNU_Student_Portal_DbContext>();

        // Uses the exact same key your Program.cs uses: "DefaultConnection"
        optionsBuilder.UseNpgsql(
            configuration.GetConnectionString("DefaultConnection")
        );

        return new BNU_Student_Portal_DbContext(optionsBuilder.Options);
    }
}