using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace ApexPost.Api.Filters;

/// <summary>
/// Action filter that requires a valid X-Api-Key header for partner endpoints.
/// Keys are compared as SHA-256 hashes with <see cref="CryptographicOperations.FixedTimeEquals"/>
/// so the check is constant-time and the raw key is never compared directly.
/// Configured values under "PartnerApiKeys" may be plaintext keys or 64-char hex SHA-256 hashes.
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
        var configuredKeys = _configuration.GetSection("PartnerApiKeys").Get<string[]>() ?? Array.Empty<string>();

        if (string.IsNullOrEmpty(apiKey) || !IsValidKey(apiKey, configuredKeys))
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

    /// <summary>
    /// Hashes the presented key once, then fixed-time compares it against the SHA-256 of
    /// every configured key (or the configured value itself when it is already a hex hash).
    /// </summary>
    public static bool IsValidKey(string apiKey, IReadOnlyCollection<string> configuredKeys)
    {
        if (configuredKeys.Count == 0)
        {
            return false;
        }

        var presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        var matched = false;

        foreach (var configured in configuredKeys)
        {
            if (string.IsNullOrWhiteSpace(configured))
            {
                continue;
            }

            byte[] expectedHash;
            if (configured.Length == 64 && IsHex(configured))
            {
                // Configuration already stores a SHA-256 hash (preferred for production).
                expectedHash = Convert.FromHexString(configured);
            }
            else
            {
                expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
            }

            // Accumulate without early exit so the comparison stays constant-time.
            matched |= CryptographicOperations.FixedTimeEquals(expectedHash, presentedHash);
        }

        return matched;
    }

    private static bool IsHex(string value)
    {
        foreach (var c in value)
        {
            if (!Uri.IsHexDigit(c))
            {
                return false;
            }
        }
        return true;
    }
}
