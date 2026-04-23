using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Persistence.Data.DbContext;
using Microsoft.EntityFrameworkCore;

namespace BNU_Student_Portal_Web.Extensions
{
    public static class WebAppRegistrations
    {
        public static async Task<WebApplication> MigrateDatabaseAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<BNU_Student_Portal_DbContext>();

            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await dbContext.Database.MigrateAsync();
            }

            return app;
        }

        public static async Task<WebApplication> SeedIdentityDataAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dataInitializer = scope.ServiceProvider.GetRequiredService<IDataInitializer>(); //to create initial data and auto migrations 
            await dataInitializer.Initialize();
            return app;
        }


    }
}
