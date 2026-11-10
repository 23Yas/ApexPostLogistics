using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ApexPost.Api;

/// <summary>
/// Sets the OpenAPI document info (title, description, version, contact),
/// declares the Bearer security scheme, and filters each named document
/// ("v1" / "v2") to only the routes of that API version.
/// </summary>
public sealed class ApexPostOpenApiTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var isV2 = string.Equals(context.DocumentName, "v2", StringComparison.OrdinalIgnoreCase);
        var routePrefix = isV2 ? "/api/v2/" : "/api/v1/";

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

        // Keep only the paths belonging to this document's API version.
        foreach (var path in document.Paths.Keys.Where(p => !p.StartsWith(routePrefix, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            document.Paths.Remove(path);
        }

        // v1 is deprecated: mark every operation so clients and Swagger UI show it.
        if (!isV2)
        {
            foreach (var pathItem in document.Paths.Values)
            {
                foreach (var operation in pathItem.Operations.Values)
                {
                    operation.Deprecated = true;
                }
            }
        }

        // Global Bearer security requirement so Swagger UI's Authorize button applies to all operations.
        document.Security =
        [
            new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer")] = [] }
        ];

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
