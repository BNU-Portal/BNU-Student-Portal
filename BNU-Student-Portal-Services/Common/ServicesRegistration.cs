using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using BNU_Student_Portal_Services.Common.Behaviors;
using BNU_Student_Portal_Services.FluentValidationMiddleWare;
using BNU_Student_Portal_Services_Implementation;
using BNU_Student_Portal_Services.Common.Behaviors.Email;
using BNU_Student_Portal_Services.Common.Behaviors.Upload;
using BNU_Student_Portal_Services.Features.Authentication;
using BNU_Student_Portal_Shared_Library.Settings;
using CloudinaryDotNet;
using Microsoft.Extensions.Configuration;

public static class ServicesRegistration
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services ,
        IConfiguration configuration)
    {
        var assembly = typeof(ServicesRegistration).Assembly;

        #region Upload
        services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));
        services.AddScoped<IUploadService, UploadService>();
        // 2 — Initialize Cloudinary SDK and register as singleton
        var cloudinarySettings = configuration
            .GetSection("Cloudinary")
            .Get<CloudinarySettings>();

        var account = new Account(
            cloudinarySettings.CloudName,
            cloudinarySettings.ApiKey,
            cloudinarySettings.ApiSecret
        );

        var cloudinary = new Cloudinary(account);
        cloudinary.Api.Secure = true; // always use https

        services.AddSingleton(cloudinary);

        #endregion

        #region Email
        // Bind EmailSettings from appsettings.json
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<AppSettings>(configuration.GetSection("URLS"));
        // Register the email service
        services.AddTransient<IEmailService, EmailService>();


        // Register the background queue as Singleton (must survive the app lifetime)
        services.AddSingleton<BackgroundEmailQueue>();

        // Register the hosted worker
        services.AddHostedService<EmailSenderBackgroundService>();

        #endregion


        services.AddScoped<IAuthenticationService,AuthenticationService>();

        services.AddAutoMapper(assembly);

        // Registers ALL handlers in this assembly automatically
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(assembly));

        // Registers ALL validators in this assembly automatically
        // CreateProductCommandValidator gets picked up here without you doing anything
        services.AddValidatorsFromAssembly(assembly);

        // Register the pipeline — runs for EVERY command/query
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        //using transient because we want a new instance for each request and its stateless nature, and change in each request.
        //If we used singleton, it would be shared across all requests, which could lead to issues in a multi-threaded environment.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}