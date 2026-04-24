using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Persistence.Data.DataSeed;
using BNU_Student_Portal_Persistence.Data.DbContext;
using BNU_Student_Portal_Persistence.DI;
using BNU_Student_Portal_Presentation;
using BNU_Student_Portal_Services.FluentValidationMiddleWare;
using BNU_Student_Portal_Web.CustomMiddlewares;
using BNU_Student_Portal_Web.Extensions;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(PresentationAssemblyMarker).Assembly);

#region OpenAPI / Scalar
builder.Services.AddOpenApi();
#endregion

#region Dependency Injection

#region DB
// NOTE: Make sure to add the matching EF Core provider NuGet package to BNU-Student-Portal-Persistence.csproj
//      Npgsql.EntityFrameworkCore.PostgreSQL    --> UseNpgsql
builder.Services.AddDbContext<BNU_Student_Portal_DbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion


#region Data Initializer

builder.Services.AddScoped<IDataInitializer, DataInitializer>();

#endregion

#region Application Services
builder.Services.AddApplicationServices();
#endregion

#region Persistence
builder.Services.AddPersistenceServicesRegistration();
#endregion



#endregion

var app = builder.Build();

app.UseMiddleware<ExceptionHandlerMiddleware>();


//adding custom middlewares for database migration and data seeding during application startup
await app.MigrateDatabaseAsync();

// Seed initial data (e.g., roles, default users) after migrations
await app.SeedIdentityDataAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();            // serves → /openapi/v1.json
    app.MapScalarApiReference(); // UI → /scalar/v1
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
