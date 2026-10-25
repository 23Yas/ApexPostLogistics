namespace ApexPost.Web.Controllers;

using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public sealed class ReportsController : Controller
{
    private readonly ReportsService _reportsService;

    public ReportsController(ReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [HttpGet]
    public async Task<IActionResult> Daily(CancellationToken cancellationToken)
    {
        var branchCode = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        var transactions = await _reportsService.GetTransactionsAsync(branchCode, cancellationToken);
        var total = transactions.Sum(item => item.Total.Amount);
        ViewBag.Total = total;
        ViewBag.BranchCode = branchCode.Value;
        return View(transactions);
    }

    [HttpGet]
    public async Task<IActionResult> LowStock(CancellationToken cancellationToken)
    {
        var branchCode = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        var items = await _reportsService.GetLowStockAsync(branchCode, cancellationToken);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Shipping(CancellationToken cancellationToken)
    {
        var branchCode = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        var transactions = await _reportsService.GetTransactionsAsync(branchCode, cancellationToken);
        var shippingSummary = transactions
            .SelectMany(t => t.LineItems)
            .Where(item => !string.IsNullOrWhiteSpace(item.ServiceCode))
            .GroupBy(item => item.ServiceCode)
            .Select(group => new
            {
                ServiceCode = group.Key,
                Count = group.Count(),
                TotalValue = group.Sum(item => item.UnitPrice.Amount * item.Quantity)
            })
            .ToList();

        ViewBag.BranchCode = branchCode.Value;
        return View(shippingSummary);
    }

    [HttpGet]
    public async Task<IActionResult> MoneyOrders(CancellationToken cancellationToken)
    {
        var branchCode = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        var transactions = await _reportsService.GetTransactionsAsync(branchCode, cancellationToken);
        var summary = transactions
            .Select(t => new
            {
                t.Id,
                t.CounterId,
                t.Status,
                Total = t.Total.Amount,
                CreatedAtUtc = t.CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd")
            })
            .ToList();

        ViewBag.BranchCode = branchCode.Value;
        return View(summary);
    }
}
