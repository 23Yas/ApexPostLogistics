namespace ApexPost.Web.Controllers;

using System.Diagnostics;
using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;
using ApexPost.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public sealed class HomeController : Controller
{
    private readonly CounterService _counterService;
    private readonly ReportsService _reportsService;
    private readonly IStaffRepository _staffRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockRepository _stockRepository;

    public HomeController(
        CounterService counterService,
        ReportsService reportsService,
        IStaffRepository staffRepository,
        IProductRepository productRepository,
        IStockRepository stockRepository)
    {
        _counterService = counterService;
        _reportsService = reportsService;
        _staffRepository = staffRepository;
        _productRepository = productRepository;
        _stockRepository = stockRepository;
    }

    [Authorize]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return await Dashboard(cancellationToken);
    }

    [Authorize]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var branchCode = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");

        var branchTask = _counterService.GetBranchAsync(branchCode, cancellationToken);
        var lowStockTask = _reportsService.GetLowStockAsync(branchCode, cancellationToken);
        var transactionsTask = _reportsService.GetTransactionsAsync(branchCode, cancellationToken);
        var staffTask = _staffRepository.GetAllAsync(cancellationToken);
        var productsTask = _productRepository.GetCachedProductsAsync(cancellationToken).AsTask();
        var stockItemsTask = _stockRepository.GetAllAsync(cancellationToken);

        await Task.WhenAll(branchTask, lowStockTask, transactionsTask, staffTask, productsTask, stockItemsTask);

        var branch = await branchTask;
        var lowStock = await lowStockTask;
        var transactions = await transactionsTask;
        var staff = await staffTask;
        var products = await productsTask;
        var stockItems = await stockItemsTask;
        var branchStock = stockItems.Where(x => x.BranchCode == branchCode).ToList();
        var salesToday = transactions.Sum(item => item.Total.Amount);

        ViewBag.BranchName = branch?.Name ?? "Unknown Branch";
        ViewBag.BranchCode = branchCode.Value;
        ViewBag.StaffCount = staff.Count(item => item.HomeBranch == branchCode);
        ViewBag.LowStockCount = lowStock.Count;
        ViewBag.SalesToday = salesToday;
        ViewBag.TransactionCount = transactions.Count;
        ViewBag.CatalogueCount = products.Count();
        ViewBag.ProductRows = branchStock
            .Select(item => new
            {
                ProductSku = item.ProductSku.Value,
                ProductName = products.FirstOrDefault(p => p.Sku == item.ProductSku)?.Name ?? item.ProductSku.Value,
                QuantityOnHand = item.QuantityOnHand,
                ReorderThreshold = item.ReorderThreshold,
                Status = item.QuantityOnHand <= item.ReorderThreshold ? "Low" : "Healthy"
            })
            .ToList();

        return View();
    }

    [Authorize]
    public async Task<IActionResult> Catalogue(CancellationToken cancellationToken)
    {
        var branchCode = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        var products = await _productRepository.GetCachedProductsAsync(cancellationToken);
        var stockItems = await _stockRepository.GetAllAsync(cancellationToken);
        var rows = products
            .Select(product => new
            {
                Product = product,
                QuantityOnHand = stockItems.FirstOrDefault(x => x.BranchCode == branchCode && x.ProductSku == product.Sku)?.QuantityOnHand ?? 0,
                IsLowStock = (stockItems.FirstOrDefault(x => x.BranchCode == branchCode && x.ProductSku == product.Sku)?.QuantityOnHand ?? 0) <= 5
            })
            .OrderBy(x => x.Product.Name)
            .ToList();

        return View(rows);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
