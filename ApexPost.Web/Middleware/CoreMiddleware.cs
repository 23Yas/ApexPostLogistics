namespace ApexPost.Web.Middleware;

using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;

public sealed class CorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

    public string? Current => CurrentCorrelationId.Value;

    public void Set(string? correlationId) => CurrentCorrelationId.Value = correlationId;
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequireApiKeyAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequireAllowedIpAttribute : Attribute
{
}

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CorrelationIdAccessor _accessor;

    public CorrelationIdMiddleware(RequestDelegate next, CorrelationIdAccessor accessor)
    {
        _next = next;
        _accessor = accessor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        var correlationId = string.IsNullOrWhiteSpace(incoming) ? Guid.NewGuid().ToString("N") : incoming;
        context.TraceIdentifier = correlationId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        _accessor.Set(correlationId);

        await _next(context);
    }
}

public sealed class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var start = DateTimeOffset.UtcNow;
        try
        {
            await _next(context);
        }
        finally
        {
            var duration = DateTimeOffset.UtcNow - start;
            var correlationId = context.TraceIdentifier;
            _logger.LogInformation("Request {Method} {Path} completed in {DurationMs}ms with status {StatusCode}. CorrelationId={CorrelationId}", context.Request.Method, context.Request.Path, duration.TotalMilliseconds, context.Response.StatusCode, correlationId);
        }
    }
}

public sealed class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public MaintenanceModeMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (_configuration.GetValue<bool>("AppSettings:MaintenanceMode"))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Title = "Service unavailable", Detail = "The platform is under maintenance. Please try again shortly.", Status = StatusCodes.Status503ServiceUnavailable });
            return;
        }

        await _next(context);
    }
}

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; object-src 'none'; frame-ancestors 'none'; base-uri 'self';";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public static class EndpointMetadataValidator
{
    public static bool HasMetadata(HttpContext context, Type metadataType)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            return false;
        }

        return endpoint.Metadata.Any(item => item.GetType() == metadataType || item.GetType().IsSubclassOf(metadataType));
    }
}

public sealed class ApiKeyValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public ApiKeyValidationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!EndpointMetadataValidator.HasMetadata(context, typeof(RequireApiKeyAttribute)))
        {
            await _next(context);
            return;
        }

        var configuredKey = _configuration["Integration:ApiKey"] ?? _configuration["AppSettings:IntegrationApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Title = "Configuration error", Detail = "The integration API key is not configured.", Status = StatusCodes.Status500InternalServerError });
            return;
        }

        var requestKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(requestKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Title = "Unauthorized", Detail = "Missing API key.", Status = StatusCodes.Status401Unauthorized });
            return;
        }

        var isValid = FixedTimeEquals(requestKey, configuredKey);
        if (!isValid)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Title = "Unauthorized", Detail = "Invalid API key.", Status = StatusCodes.Status401Unauthorized });
            return;
        }

        await _next(context);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        if (leftBytes.Length != rightBytes.Length)
        {
            return false;
        }

        var result = 0;
        for (var i = 0; i < leftBytes.Length; i++)
        {
            result |= leftBytes[i] ^ rightBytes[i];
        }

        return result == 0;
    }
}

public sealed class AllowedIpMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public AllowedIpMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!EndpointMetadataValidator.HasMetadata(context, typeof(RequireAllowedIpAttribute)))
        {
            await _next(context);
            return;
        }

        var allowedIps = (_configuration["Firewall:AllowedIps"] ?? "127.0.0.1,::1").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is null || !allowedIps.Any(ip => IPAddress.Parse(ip).Equals(remoteIp) || IPAddress.Parse(ip).Equals(context.Connection.LocalIpAddress)))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Title = "Forbidden", Detail = "Your IP address is not allowed.", Status = StatusCodes.Status403Forbidden });
            return;
        }

        await _next(context);
    }
}

public sealed class RateLimitMiddleware
{
    private static readonly ConcurrentDictionary<string, int> RequestCounts = new();
    private readonly RequestDelegate _next;

    public RateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/Account/Login", StringComparison.OrdinalIgnoreCase))
        {
            var key = $"{context.Connection.RemoteIpAddress}:{context.Request.Path}";
            var current = RequestCounts.AddOrUpdate(key, 1, (_, value) => value + 1);
            if (current > 5)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = "60";
                await context.Response.WriteAsJsonAsync(new ProblemDetails { Title = "Too many requests", Detail = "Login rate limit exceeded.", Status = StatusCodes.Status429TooManyRequests });
                return;
            }
        }

        if (context.Request.Path.StartsWithSegments("/integration", StringComparison.OrdinalIgnoreCase))
        {
            var key = $"{context.Connection.RemoteIpAddress}:integration";
            var current = RequestCounts.AddOrUpdate(key, 1, (_, value) => value + 1);
            if (current > 30)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = "60";
                await context.Response.WriteAsJsonAsync(new ProblemDetails { Title = "Too many requests", Detail = "Partner rate limit exceeded.", Status = StatusCodes.Status429TooManyRequests });
                return;
            }
        }

        await _next(context);
    }
}

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Path}. CorrelationId {CorrelationId}", context.Request.Path, context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "We were unable to complete your request. Please try again later.",
                Extensions = { ["traceId"] = context.TraceIdentifier }
            });
        }
    }
}
