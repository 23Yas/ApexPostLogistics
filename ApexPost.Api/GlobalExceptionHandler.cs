using System.Text.Json;
using ApexPost.Core.Domain;
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
        var (statusCode, title, detail) = exception switch
        {
            // Domain failures carry their own status/title; the message is safe to return.
            HttpDomainException domain => (domain.StatusCode, domain.Title, domain.Message),
            _ => (StatusCodes.Status500InternalServerError, "An error occurred",
                _env.IsDevelopment()
                    ? exception.Message
                    : "An internal server error occurred. Please try again later.")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with {StatusCode}: {Message}", statusCode, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = detail,
            Type = statusCode >= StatusCodes.Status500InternalServerError
                ? "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                : $"https://tools.ietf.org/html/rfc7231#section-6.5.{statusCode % 100}"
        };

        if (_env.IsDevelopment() && statusCode >= StatusCodes.Status500InternalServerError)
        {
            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
            problemDetails.Extensions["exception"] = exception.ToString();
        }

        httpContext.Response.StatusCode = statusCode;
        // WriteAsJsonAsync would force application/json; serialize manually so the
        // response keeps the RFC 7807 application/problem+json content type.
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, JsonOptions), cancellationToken);

        return true;
    }
}
