using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

namespace ApexPost.Api;

/// <summary>
/// Reads the API version from a literal "/api/v{n}/" path segment
/// (e.g. "v1" -> "1", "v2" -> "2"). Keeps the explicit route templates
/// readable while letting Asp.Versioning select the right controller.
/// </summary>
public sealed class LiteralSegmentApiVersionReader : IApiVersionReader, IApiVersionParameterSource
{
    public IReadOnlyList<string> Read(HttpRequest request)
    {
        var segments = request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var segment = segments?.FirstOrDefault(s =>
            s.Length > 1 && s[0] == 'v' && char.IsDigit(s[1]));

        return segment is null ? [] : new[] { segment[1..] };
    }

    // Version is already explicit in the URL segment; nothing extra to document.
    public void AddParameters(IApiVersionParameterDescriptionContext context) { }
}
