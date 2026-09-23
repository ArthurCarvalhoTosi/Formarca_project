using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Formarca.Web.RateLimiting;

public static class RateLimitingExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IServiceCollection AddFormarcaRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectedAsync;

            options.AddPolicy(RateLimitingPolicies.Login, httpContext =>
                FixedWindowPartition(httpContext, settings.Login));

            options.AddPolicy(RateLimitingPolicies.Refresh, httpContext =>
                FixedWindowPartition(httpContext, settings.Refresh));

            options.AddPolicy(RateLimitingPolicies.Registration, httpContext =>
                FixedWindowPartition(httpContext, settings.Registration));

            options.AddPolicy(RateLimitingPolicies.Webhook, httpContext =>
                FixedWindowPartition(httpContext, settings.Webhook));

            options.AddPolicy(RateLimitingPolicies.Health, httpContext =>
                FixedWindowPartition(httpContext, settings.Health));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var key = !string.IsNullOrEmpty(userId)
                    ? $"user:{userId}"
                    : $"ip:{GetClientIp(httpContext)}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    key,
                    _ => CreateFixedWindowOptions(settings.Global));
            });
        });

        return services;
    }

    private static RateLimitPartition<string> FixedWindowPartition(
        HttpContext httpContext,
        RateLimitWindowOptions window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientIp(httpContext),
            _ => CreateFixedWindowOptions(window));

    private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(RateLimitWindowOptions window) =>
        new()
        {
            PermitLimit = window.PermitLimit,
            Window = TimeSpan.FromSeconds(window.WindowSeconds),
            QueueLimit = 0
        };

    private static string GetClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static async ValueTask WriteRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(
            new
            {
                title = "Too many requests",
                detail = "Rate limit exceeded. Try again later."
            },
            JsonOptions);

        await response.WriteAsync(payload, cancellationToken);
    }
}
