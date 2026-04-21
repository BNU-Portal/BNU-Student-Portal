using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using BNU_Student_Portal_Persistence.Data.DbContext;
using BNU_Student_Portal_Persistence.DI;
using BNU_Student_Portal_Services.FluentValidationMiddleWare;
using BNU_Student_Portal_Web.CustomMiddlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

#region OpenAPI / Scalar
builder.Services.AddOpenApi();
#endregion

#region Dependency Injection

#region DB
// NOTE: Make sure to add the matching EF Core provider NuGet package to BNU-Student-Portal-Persistence.csproj
// e.g. Microsoft.EntityFrameworkCore.SqlServer  --> UseSqlServer
//      Npgsql.EntityFrameworkCore.PostgreSQL    --> UseNpgsql
builder.Services.AddDbContext<BNUStudentPortalDbContext>(options =>
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
    app.MapOpenApi();            // serves -> /openapi/v1.json
    app.MapScalarApiReference(); // UI -> /scalar/v1
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
