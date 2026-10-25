using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Application.Services;
using ApexPost.Infrastructure;
using ApexPost.Web.Filters;
using ApexPost.Web.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
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

builder.Services.AddSingleton<CorrelationIdAccessor>();
builder.Services.AddSingleton<IAuditStore>(_ => new JsonAuditStore(Path.Combine(AppContext.BaseDirectory, "data", "audit.json")));
builder.Services.AddSingleton<IBranchRepository>(_ => new BranchRepository(Path.Combine(AppContext.BaseDirectory, "data", "branches.json")));
builder.Services.AddSingleton<IStaffRepository>(_ => new StaffRepository(Path.Combine(AppContext.BaseDirectory, "data", "staff.json")));
builder.Services.AddSingleton<IProductRepository>(_ => new ProductRepository(Path.Combine(AppContext.BaseDirectory, "data", "products.json")));
builder.Services.AddSingleton<IStockRepository>(_ => new StockRepository(Path.Combine(AppContext.BaseDirectory, "data", "stock.json")));
builder.Services.AddSingleton<ITransactionRepository>(_ => new TransactionRepository(Path.Combine(AppContext.BaseDirectory, "data", "transactions.json")));
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

await SeedDataBootstrap.InitializeAsync(app.Services);

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
