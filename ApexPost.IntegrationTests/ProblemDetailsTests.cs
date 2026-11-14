namespace ApexPost.Tests;

using ApexPost.Core.Domain;
using ApexPost.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class ProblemDetailsTests
{
    private static async Task<DefaultHttpContext> RunAsync(Exception toThrow)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw toThrow,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        return context;
    }

    [Fact]
    public async Task DomainError_returns_400_as_problem_details()
    {
        var context = await RunAsync(new DomainException("Insufficient stock for SKU SKU-1001."));

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
    }

    [Fact]
    public async Task ConcurrencyConflict_returns_409_as_problem_details()
    {
        var context = await RunAsync(new DbUpdateConcurrencyException("Stock changed by another clerk."));

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
    }
}