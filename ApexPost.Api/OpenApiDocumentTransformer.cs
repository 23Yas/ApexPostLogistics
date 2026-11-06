using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ApexPost.Api;

/// <summary>
/// Sets the OpenAPI document info (title, description, version, contact)
/// and declares the Bearer security scheme used by JWT-authenticated endpoints.
/// </summary>
public sealed class ApexPostOpenApiTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var isV2 = context.DocumentName.Contains("v2", StringComparison.OrdinalIgnoreCase);

        document.Info.Title = "ApexPost Logistics API";
        document.Info.Description = isV2
            ? "RESTful API for ApexPost Logistics shipment tracking and management (v2)"
            : "RESTful API for ApexPost Logistics shipment tracking and management (v1 - deprecated)";
        document.Info.Version = isV2 ? "v2" : "v1";
        document.Info.Contact = new OpenApiContact
        {
            Name = "ApexPost API Team",
            Email = "api-team@apexpost.local"
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Bearer token issued for couriers and admins"
        };

        return Task.CompletedTask;
    }
}
