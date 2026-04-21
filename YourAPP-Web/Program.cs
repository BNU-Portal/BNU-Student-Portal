using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using YourAPP_Persistence.Data.DbContext;
using YourAPP_Persistence.DI;
using YourAPP_Services.FluentValidationMiddleWare;
using YourAPP_Web.CustomMiddlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

#region OpenAPI / Scalar
builder.Services.AddOpenApi();
#endregion

#region Dependency Injection

#region DB
// NOTE: Make sure to add the matching EF Core provider NuGet package to YourAPP-Persistence.csproj
// e.g. Microsoft.EntityFrameworkCore.SqlServer  --> UseSqlServer
//      Npgsql.EntityFrameworkCore.PostgreSQL    --> UseNpgsql
builder.Services.AddDbContext<YourAPPDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();            // serves → /openapi/v1.json
    app.MapScalarApiReference(); // UI → /scalar/v1
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
