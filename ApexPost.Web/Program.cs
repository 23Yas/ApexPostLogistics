using System.Net;
using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Application.Services;
using ApexPost.Infrastructure;
using ApexPost.Web.Filters;
using ApexPost.Web.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("PartnerTrackingWidget", policy =>
    {
        policy.WithOrigins("https://tracking-widget.apexpost.local", "https://partner.apexpost.local")
              .AllowAnyHeader()
              .WithMethods("GET");
    });
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AuditActionFilter>();
    options.Filters.Add<ValidationFilter>();
    options.Filters.Add<PerformanceFilter>();
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "ApexPostAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdministratorOnly", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("BranchManagerOnly", policy => policy.RequireRole("BranchManager"));
    options.AddPolicy("SameBranchOnly", policy => policy.RequireAssertion(context =>
    {
        if (context.Resource is HttpContext httpContext)
        {
            var routeValues = httpContext.Request.RouteValues;
            var userBranch = context.User.FindFirst("BranchCode")?.Value;
            var requestedBranch = routeValues.TryGetValue("branchCode", out var value) ? value?.ToString() : null;
            return string.IsNullOrWhiteSpace(userBranch) || string.IsNullOrWhiteSpace(requestedBranch) || userBranch == requestedBranch;
        }

        return false;
    }));
});

builder.Services.AddDbContext<ApexPostDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ApexPostDb");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ApexPostDb connection string is missing. Set it via dotnet user-secrets or the environment configuration.");
    }

    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure();
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddSingleton<CorrelationIdAccessor>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBranchRepository, EfBranchRepository>();
builder.Services.AddScoped<IStaffRepository, EfStaffRepository>();
builder.Services.AddScoped<IProductRepository, EfProductRepository>();
builder.Services.AddScoped<IStockRepository, EfStockRepository>();
builder.Services.AddScoped<ITransactionRepository, EfTransactionRepository>();
builder.Services.AddScoped<IMoneyOrderRepository, EfMoneyOrderRepository>();
builder.Services.AddScoped<IAuditStore, EfAuditStore>();
builder.Services.AddSingleton<INotificationSink, SimulatedNotificationSink>();
builder.Services.AddSingleton<IShippingRateProvider, CourierRateProvider>();

builder.Services.AddScoped<CounterService>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<ReportsService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<StaffService>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<MaintenanceModeMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApexPostDbContext>();
    dbContext.Database.Migrate();
    await SeedDataBootstrap.InitializeAsync(dbContext);
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("PartnerTrackingWidget");
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<ApiKeyValidationMiddleware>();
app.UseMiddleware<AllowedIpMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
