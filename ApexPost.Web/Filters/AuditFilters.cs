namespace ApexPost.Web.Filters;

using System.Diagnostics;
using System.Security.Claims;
using ApexPost.Core.Application.Interfaces;
using ApexPost.Web.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

public sealed class AuditActionFilter : IAsyncActionFilter
{
    private readonly IAuditStore _auditStore;
    private readonly CorrelationIdAccessor _correlationIdAccessor;
    private readonly ILogger<AuditActionFilter> _logger;

    public AuditActionFilter(IAuditStore auditStore, CorrelationIdAccessor correlationIdAccessor, ILogger<AuditActionFilter> logger)
    {
        _auditStore = auditStore;
        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var actionResult = await next();
        stopwatch.Stop();

        var staffIdClaim = context.HttpContext.User.FindFirst("StaffId")?.Value ?? Guid.Empty.ToString();
        var staffId = Guid.TryParse(staffIdClaim, out var parsedStaffId) ? parsedStaffId : Guid.Empty;
        var role = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value ?? context.HttpContext.User.FindFirst("Role")?.Value ?? "Anonymous";
        var branchCode = context.HttpContext.User.FindFirst("BranchCode")?.Value ?? "unknown";
        var controller = context.Controller is ControllerBase controllerBase ? controllerBase.GetType().Name : nameof(ActionExecutingContext);
        var action = context.ActionDescriptor is ControllerActionDescriptor actionDescriptor ? actionDescriptor.ActionName : "Unknown";
        var routeValues = string.Join(';', context.RouteData.Values.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var status = actionResult.Result switch
        {
            null => context.HttpContext.Response.StatusCode.ToString(),
            IStatusCodeActionResult codeResult => codeResult.StatusCode?.ToString() ?? "200",
            _ => context.HttpContext.Response.StatusCode.ToString()
        };

        var correlationId = _correlationIdAccessor.Current ?? context.HttpContext.TraceIdentifier;
        var details = $"{controller}.{action} {routeValues}";
        var entry = new AuditEntry(
            Guid.NewGuid(),
            staffId,
            role,
            branchCode,
            controller,
            action,
            correlationId,
            status,
            routeValues,
            stopwatch.Elapsed.TotalMilliseconds,
            DateTimeOffset.UtcNow,
            details,
            ["password", "apiKey", "documentNumber", "cardData"]);

        await _auditStore.AppendAsync(entry);
        _logger.LogInformation("Audit entry recorded for {Controller}.{Action} correlation {CorrelationId}.", controller, action, correlationId);
    }
}

public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly ILogger<ValidationFilter> _logger;

    public ValidationFilter(ILogger<ValidationFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            _logger.LogWarning("Validation failed for {Controller}.{Action}", context.Controller.GetType().Name, context.ActionDescriptor.DisplayName);
            if (context.Controller is Controller controller)
            {
                context.Result = new BadRequestObjectResult(context.ModelState);
                return;
            }

            context.Result = new BadRequestResult();
            return;
        }

        await next();
    }
}

public sealed class PerformanceFilter : IAsyncActionFilter
{
    private readonly ILogger<PerformanceFilter> _logger;
    private readonly IConfiguration _configuration;

    public PerformanceFilter(ILogger<PerformanceFilter> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var thresholdMs = _configuration.GetValue<int>("AppSettings:PerformanceWarningThresholdMs", 1000);
        var stopwatch = Stopwatch.StartNew();
        var result = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > thresholdMs)
        {
            _logger.LogWarning("Action {Controller}.{Action} exceeded threshold with {DurationMs}ms.", context.Controller.GetType().Name, context.ActionDescriptor.DisplayName, stopwatch.ElapsedMilliseconds);
        }

        if (result.Result is not null)
        {
            return;
        }
    }
}
