using BNU_Student_Portal_Persistence.Data.DbContext;
using BNU_Student_Portal_Persistence.DI;
using BNU_Student_Portal_Services.FluentValidationMiddleWare;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

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
builder.Services.AddDbContext<BNU_Student_Portal_DbContext>(options =>
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

app.UseMiddleware<BNU_Student_Portal_Web.CustomMiddlewares.ExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();            // serves → /openapi/v1.json
    app.MapScalarApiReference(); // UI → /scalar/v1
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
