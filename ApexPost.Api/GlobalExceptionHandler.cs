using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Api;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _env;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Title = "An error occurred",
            Status = StatusCodes.Status500InternalServerError,
            Detail = _env.IsDevelopment()
                ? exception.Message
                : "An internal server error occurred. Please try again later.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

        if (_env.IsDevelopment())
        {
            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
            problemDetails.Extensions["exception"] = exception.ToString();
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        // WriteAsJsonAsync would force application/json; serialize manually so the
        // response keeps the RFC 7807 application/problem+json content type.
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, JsonOptions), cancellationToken);

        return true;
    }
}
