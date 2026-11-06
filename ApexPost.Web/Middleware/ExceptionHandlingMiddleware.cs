namespace ApexPost.Web.Middleware;

using ApexPost.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

/// <summary>Maps exceptions to RFC 7807 Problem Details. Replaces the old 500-only class in CoreMiddleware.cs.</summary>
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there is nothing to write.
        }
        catch (Exception ex)
        {
            var (status, title, detail) = Map(ex);

            if (status >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception for {Path}. TraceId {TraceId}", context.Request.Path, context.TraceIdentifier);
            }
            else
            {
                _logger.LogWarning("Request rejected with {Status} for {Path}: {Message}", status, context.Request.Path, ex.Message);
            }

            if (context.Response.HasStarted)
            {
                throw;
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Extensions = { ["traceId"] = context.TraceIdentifier }
            };

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem), context.RequestAborted);
        }
    }

    private static (int Status, string Title, string Detail) Map(Exception ex) => ex switch
    {
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict", "This record was changed by someone else. Reload it and try again."),
        DbUpdateException dbEx when IsUniqueViolation(dbEx) => (StatusCodes.Status409Conflict, "Conflict", "A record with the same unique value already exists."),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found", "The requested record was not found."),
        DomainException => (StatusCodes.Status400BadRequest, "Business rule violated", ex.Message),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "We were unable to complete your request. Please try again later.")
    };

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is SqlException { Number: 2601 or 2627 };
}
