using ApexPost.Api;
using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Application.Services;
using ApexPost.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<ApexPostDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ApexPostDb")));

// Register repositories
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register services
builder.Services.AddScoped<ShipmentService>();

// Add controllers
builder.Services.AddControllers();

// Add JWT Bearer authentication. Signing key/issuer/audience come from configuration
// or User Secrets - never committed. A well-known dev key is used only in Development.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Jwt:Key must be configured outside Development (use User Secrets or environment variables).");
    }
    jwtKey = "dev-only-signing-key-not-for-production-32chars";
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "apexpost-dev";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "apexpost-courier";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Built-in OpenAPI generator (single generator - Swagger UI only renders it).
// Two named documents; the transformer filters each to its own /api/v{n} routes.
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<ApexPostOpenApiTransformer>();
});
builder.Services.AddOpenApi("v2", options =>
{
    options.AddDocumentTransformer<ApexPostOpenApiTransformer>();
});

// Swagger UI hosts the interactive docs; it reads the built-in OpenAPI documents.
builder.Services.AddEndpointsApiExplorer();

// Add global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);

var app = builder.Build();

// API version headers (URL-segment versioning: /api/v1, /api/v2)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("api-supported-versions", "1.0, 2.0");
    context.Response.Headers.Append("api-deprecated-versions", "1.0");
    await next();
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json");
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1 (deprecated)");
        options.SwaggerEndpoint("/openapi/v2.json", "v2");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
