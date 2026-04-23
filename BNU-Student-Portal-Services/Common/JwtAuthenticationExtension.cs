using BNU_Student_Portal_Shared_Library.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;


public static class JwtAuthenticationExtension
{
    public static IServiceCollection AddJwtAuthentication(
    this IServiceCollection services, IConfiguration config)
    {
        var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>()!;
        services.Configure<JwtSettings>(config.GetSection("JwtSettings"));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                                               Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ClockSkew = TimeSpan.Zero // no extra 5 min tolerance
            };
        });

        return services;
    }
}