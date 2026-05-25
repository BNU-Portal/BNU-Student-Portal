using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

    
namespace BNU_Student_Portal_Presentation.Filters;

/// <summary>
/// User-scoped Redis cache attribute.
/// Cache key = "{prefix}:{userId}:{query params}"
/// Each user gets their own isolated cache — no cross-user leakage.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RedisCacheAttribute(
    string prefix,
    int ttlSeconds = 60) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var redis = context.HttpContext.RequestServices
            .GetRequiredService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();

        // Build user-scoped cache key
        var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var queryString = context.HttpContext.Request.QueryString.Value ?? "";
        var cacheKey = $"{prefix}:{userId}:{queryString}";

        // Try cache hit first
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            context.Result = new ContentResult
            {
                Content = cached.ToString(),
                ContentType = "application/json",
                StatusCode = 200
            };
            return;
        }

        // Cache miss — execute the action
        var executed = await next();

        // Only cache successful responses
        if (executed.Result is ObjectResult { StatusCode: 200 or null } objectResult)
        {
            var serialized = JsonSerializer.Serialize(objectResult.Value);
            await db.StringSetAsync(cacheKey, serialized, TimeSpan.FromSeconds(ttlSeconds));
        }
    }
}