using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BNU_Student_Portal_Services.Common.Behaviors.Email;

/// <summary>
/// A long-running background service that continuously listens for queued email jobs
/// and executes them asynchronously, decoupled from the HTTP request pipeline.
/// Inherits from <see cref="BackgroundService"/> which is automatically started
/// and stopped by the .NET host lifecycle.
/// </summary>
public class EmailSenderBackgroundService(
    BackgroundEmailQueue queue,
    ILogger<EmailSenderBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[EmailWorker] Background email sender started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // BLOCKS here — waits idly (no CPU waste) until a job is dropped
                // into the queue by any service (OtpService, AuthService, etc.)
                var job = await queue.DequeueAsync(stoppingToken);

                logger.LogInformation("[EmailWorker] Picked up email job, executing...");

                // Execute the job — this calls emailService.SendAsync(...) internally.
                await job(stoppingToken);

                logger.LogInformation("[EmailWorker] Email job completed successfully.");
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("[EmailWorker] Shutdown requested, stopping email worker.");
                break;
            }
            catch (Exception ex)
            {
                // Any SMTP failure (wrong credentials, network timeout, etc.) lands here.
                // We log the FULL exception with stack trace so it is visible in the console.
                // We do NOT rethrow — that would kill the worker permanently.
                logger.LogError(ex,
                    "[EmailWorker] FAILED to send email. " +
                    "Exception type: {ExType} | Message: {ExMessage}",
                    ex.GetType().Name, ex.Message);
            }
        }

        logger.LogInformation("[EmailWorker] Background email sender stopped.");
    }
}
