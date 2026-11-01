namespace ApexPost.Web.Controllers;

using System.Security.Claims;
using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public sealed class TransactionsController : Controller
{
    private readonly CounterService _counterService;
    private readonly TransactionService _transactionService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMoneyOrderRepository _moneyOrderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockRepository _stockRepository;
    private readonly PricingService _pricingService;

    public TransactionsController(
        CounterService counterService,
        TransactionService transactionService,
        ITransactionRepository transactionRepository,
        IMoneyOrderRepository moneyOrderRepository,
        IProductRepository productRepository,
        IStockRepository stockRepository,
        PricingService pricingService)
    {
        _counterService = counterService;
        _transactionService = transactionService;
        _transactionRepository = transactionRepository;
        _moneyOrderRepository = moneyOrderRepository;
        _productRepository = productRepository;
        _stockRepository = stockRepository;
        _pricingService = pricingService;
    }

    [HttpGet]
    public async Task<IActionResult> New(CancellationToken cancellationToken)
    {
        var branchCode = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        var stocks = await _counterService.GetStocksAsync(branchCode, cancellationToken);
        var products = await _productRepository.GetAllAsync(cancellationToken);
        ViewBag.ProductRows = products
            .Select(product => new
            {
                Product = product,
                QuantityOnHand = stocks.FirstOrDefault(x => x.ProductSku == product.Sku)?.QuantityOnHand ?? 0
            })
            .OrderBy(x => x.Product.Name)
            .ToList();
        ViewBag.BranchCode = branchCode.Value;
        ViewBag.ServiceLevels = Enum.GetValues<ServiceLevel>().Select(value => value.ToString()).ToList();
        return View();
    }

    [HttpGet("MoneyOrder")]
    public async Task<IActionResult> MoneyOrder(CancellationToken cancellationToken)
    {
        var branchCode = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        ViewBag.BranchCode = branchCode.Value;
        ViewBag.Threshold = 1500m;
        ViewBag.ProductCatalog = await _productRepository.GetCachedProductsAsync(cancellationToken);
        ViewBag.MoneyOrders = await _moneyOrderRepository.GetByBranchAsync(branchCode, cancellationToken);
        return View();
    }

    [HttpPost("MoneyOrder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoneyOrder(decimal amount, string sender, string recipient, decimal fee, string? documentType, string? maskedIdentifier, CancellationToken cancellationToken)
    {
        var branchCode = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        ViewBag.BranchCode = branchCode.Value;
        ViewBag.Threshold = 1500m;
        ViewBag.ProductCatalog = await _productRepository.GetCachedProductsAsync(cancellationToken);

        if (amount <= 0m)
        {
            ModelState.AddModelError(nameof(amount), "The money order amount must be positive.");
            return View();
        }

        if (fee < 0m)
        {
            ModelState.AddModelError(nameof(fee), "The fee cannot be negative.");
            return View();
        }

        if (string.IsNullOrWhiteSpace(sender))
        {
            ModelState.AddModelError(nameof(sender), "The sender is required.");
            return View();
        }

        if (string.IsNullOrWhiteSpace(recipient))
        {
            ModelState.AddModelError(nameof(recipient), "The recipient is required.");
            return View();
        }

        var moneyOrder = new MoneyOrder
        {
            BranchCode = branchCode,
            Amount = new Money(amount),
            Sender = sender.Trim(),
            Recipient = recipient.Trim(),
            Fee = new Money(fee),
            RequiresIdentityVerification = BusinessRules.RequiresIdentityVerification(new MoneyOrder { Amount = new Money(amount), Sender = sender.Trim(), Recipient = recipient.Trim(), Fee = new Money(fee) })
        };

        if (moneyOrder.RequiresIdentityVerification)
        {
            if (string.IsNullOrWhiteSpace(documentType) || !IsMaskedIdentifierValid(maskedIdentifier))
            {
                ModelState.AddModelError(string.Empty, "High-value money orders require document type and a masked identifier verification.");
                return View();
            }
        }

        var persistedMoneyOrder = new MoneyOrder
        {
            BranchCode = branchCode,
            Amount = new Money(amount),
            Sender = sender.Trim(),
            Recipient = recipient.Trim(),
            Fee = new Money(fee),
            RequiresIdentityVerification = moneyOrder.RequiresIdentityVerification,
            IdentityVerificationComplete = !moneyOrder.RequiresIdentityVerification || (!string.IsNullOrWhiteSpace(documentType) && IsMaskedIdentifierValid(maskedIdentifier)),
            Status = "Approved"
        };

        await _moneyOrderRepository.AddAsync(persistedMoneyOrder, cancellationToken);
        TempData["SuccessMessage"] = moneyOrder.RequiresIdentityVerification ? $"Money order approved. Identity verification recorded for {documentType}." : "Money order recorded successfully.";
        return RedirectToAction(nameof(MoneyOrder));
    }

    [HttpGet("IdentityVerification")]
    public async Task<IActionResult> IdentityVerification(CancellationToken cancellationToken)
    {
        var branchCode = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        ViewBag.BranchCode = branchCode.Value;
        ViewBag.Threshold = 1500m;
        ViewBag.ProductCatalog = await _productRepository.GetCachedProductsAsync(cancellationToken);
        return View();
    }

    [HttpPost("IdentityVerification")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IdentityVerification(string documentType, string maskedIdentifier, string? customerName, CancellationToken cancellationToken)
    {
        var branchCode = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        ViewBag.BranchCode = branchCode.Value;
        ViewBag.Threshold = 1500m;
        ViewBag.ProductCatalog = await _productRepository.GetCachedProductsAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(documentType) || !IsMaskedIdentifierValid(maskedIdentifier))
        {
            ModelState.AddModelError(string.Empty, "Document type and a masked identifier are required.");
            return View();
        }

        TempData["SuccessMessage"] = $"Identity verification recorded for {customerName ?? "customer"}: {documentType} / {maskedIdentifier}.";
        return RedirectToAction(nameof(IdentityVerification));
    }

    [HttpGet("LabelPreview")]
    public IActionResult LabelPreview()
    {
        ViewBag.ServiceLevels = Enum.GetValues<ServiceLevel>().Select(value => value.ToString()).ToList();
        return View();
    }

    [HttpPost("LabelPreview")]
    [ValidateAntiForgeryToken]
    public IActionResult LabelPreview(string zone, string serviceLevel, decimal weightKg, bool isLetter)
    {
        var shippingQuote = new ShippingQuote
        {
            Zone = zone,
            ServiceLevel = Enum.TryParse<ServiceLevel>(serviceLevel, true, out var parsed) ? parsed : ServiceLevel.Standard,
            Weight = new Weight(weightKg),
            IsLetter = isLetter
        };

        ViewBag.ShippingQuote = shippingQuote;
        ViewBag.IsValid = PricingService.IsParcelWithinWeightLimit(shippingQuote);
        ViewBag.ServiceLevels = Enum.GetValues<ServiceLevel>().Select(value => value.ToString()).ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string productSku, int quantity, string paymentMethod, string? serviceLevel, string? counterId, CancellationToken cancellationToken)
    {
        var branchCode = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        var product = await _productRepository.GetBySkuAsync(new Sku(productSku), cancellationToken);
        if (product is null)
        {
            return BadRequest("Selected product was not found.");
        }

        var staffIdValue = User.FindFirst("StaffId")?.Value;
        if (!Guid.TryParse(staffIdValue, out var clerkId))
        {
            clerkId = Guid.Empty;
        }

        if (quantity <= 0)
        {
            ModelState.AddModelError(nameof(quantity), "Quantity must be at least 1.");
            return View("New");
        }

        var stock = await _stockRepository.GetByBranchAndSkuAsync(branchCode, product.Sku, cancellationToken);
        if (stock is null || stock.QuantityOnHand < quantity)
        {
            ModelState.AddModelError(nameof(quantity), "Requested quantity exceeds branch stock.");
            return View("New");
        }

        var lineItems = new List<TransactionLine>
        {
            new()
            {
                Description = product.Name,
                ProductSku = product.Sku,
                Quantity = quantity,
                UnitPrice = product.UnitPrice,
                TaxCategory = product.TaxCategory
            }
        };

        if (!string.IsNullOrWhiteSpace(serviceLevel) && !serviceLevel.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<ServiceLevel>(serviceLevel, true, out var parsedServiceLevel))
            {
                parsedServiceLevel = ServiceLevel.Standard;
            }

            var shippingQuote = new ShippingQuote
            {
                Zone = branchCode.Value == "BR-002" ? "South" : "North",
                ServiceLevel = parsedServiceLevel,
                Weight = new Weight(1m),
                IsLetter = false
            };
            var shippingCost = await _pricingService.CalculateShippingAsync(shippingQuote, cancellationToken);
            lineItems.Add(new TransactionLine
            {
                Description = $"Shipping - {parsedServiceLevel}",
                ServiceCode = $"SHIP-{parsedServiceLevel.ToString().ToUpperInvariant()}",
                Quantity = 1,
                UnitPrice = shippingCost,
                TaxCategory = TaxCategory.Standard
            });
        }

        var payment = Enum.TryParse<PaymentMethod>(paymentMethod, true, out var parsedPayment) ? parsedPayment : PaymentMethod.Card;
        var transaction = await _transactionService.CreateTransactionAsync(branchCode, string.IsNullOrWhiteSpace(counterId) ? "C1" : counterId, clerkId, lineItems, payment, cancellationToken);
        await _transactionService.CompleteTransactionAsync(transaction, cancellationToken);

        TempData["SuccessMessage"] = $"Transaction {transaction.Id} recorded for {product.Name}.";
        return RedirectToAction(nameof(New));
    }

    [Authorize(Policy = "BranchManagerOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(Guid transactionId, string reason, CancellationToken cancellationToken)
    {
        if (!IsBranchManager(User))
        {
            return Forbid();
        }

        var transaction = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            return NotFound();
        }

        var userBranch = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        if (transaction.BranchCode != userBranch)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            ModelState.AddModelError(nameof(reason), "A void requires a reason.");
            return RedirectToAction(nameof(HomeController.Dashboard), "Home");
        }

        var managerId = Guid.TryParse(User.FindFirst("StaffId")?.Value, out var parsedManagerId) ? parsedManagerId : Guid.Empty;
        await _transactionService.VoidAsync(transaction, managerId, reason, cancellationToken);
        return RedirectToAction(nameof(HomeController.Dashboard), "Home");
    }

    [HttpPost("Refund")]
    [Authorize(Policy = "BranchManagerOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(Guid transactionId, string reason, CancellationToken cancellationToken)
    {
        if (!IsBranchManager(User))
        {
            return Forbid();
        }

        var transaction = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            return NotFound();
        }

        var userBranch = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        if (transaction.BranchCode != userBranch)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            ModelState.AddModelError(nameof(reason), "A refund requires a reason.");
            return RedirectToAction(nameof(HomeController.Dashboard), "Home");
        }

        var managerId = Guid.TryParse(User.FindFirst("StaffId")?.Value, out var parsedManagerId) ? parsedManagerId : Guid.Empty;
        await _transactionService.RefundAsync(transaction, managerId, reason, cancellationToken);
        return RedirectToAction(nameof(HomeController.Dashboard), "Home");
    }

    private static bool IsBranchManager(ClaimsPrincipal user)
    {
        return user.Claims.Any(claim =>
            (claim.Type == ClaimTypes.Role || claim.Type == "Role") &&
            string.Equals(claim.Value, RoleType.BranchManager.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsMaskedIdentifierValid(string? maskedIdentifier)
    {
        if (string.IsNullOrWhiteSpace(maskedIdentifier))
        {
            return false;
        }

        var trimmed = maskedIdentifier.Trim();
        if (trimmed.All(char.IsDigit))
        {
            return false;
        }

        return trimmed.Length >= 4 && trimmed.Length <= 20 && trimmed.Any(ch => !char.IsDigit(ch));
    }
}
