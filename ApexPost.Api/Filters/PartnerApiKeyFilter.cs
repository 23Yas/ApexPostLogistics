using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace ApexPost.Api.Filters;

/// <summary>
/// Action filter that requires a valid X-Api-Key header for partner endpoints.
/// Valid keys are configured under the "PartnerApiKeys" configuration section.
/// Apply with <c>[ServiceFilter(typeof(PartnerApiKeyFilter))]</c>.
/// </summary>
public sealed class PartnerApiKeyFilter : IAsyncActionFilter
{
    private readonly IConfiguration _configuration;

    public PartnerApiKeyFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var apiKey = context.HttpContext.Request.Headers["X-Api-Key"].FirstOrDefault();
        var validKeys = _configuration.GetSection("PartnerApiKeys").Get<string[]>() ?? Array.Empty<string>();

        if (string.IsNullOrEmpty(apiKey) || !validKeys.Contains(apiKey))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "Invalid API key",
                Detail = "Missing or invalid X-Api-Key header",
                Status = StatusCodes.Status401Unauthorized
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                ContentTypes = { "application/problem+json" }
            };
            return;
        }

        await next();
    }
}
