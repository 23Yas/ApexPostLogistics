namespace ApexPost.Web.Controllers;

using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public sealed class TransactionsController : Controller
{
    private readonly CounterService _counterService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockRepository _stockRepository;

    public TransactionsController(
        CounterService counterService,
        ITransactionRepository transactionRepository,
        IProductRepository productRepository,
        IStockRepository stockRepository)
    {
        _counterService = counterService;
        _transactionRepository = transactionRepository;
        _productRepository = productRepository;
        _stockRepository = stockRepository;
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
        return View();
    }

    [HttpPost("MoneyOrder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoneyOrder(decimal amount, string sender, string recipient, decimal fee, string? documentType, string? maskedIdentifier, CancellationToken cancellationToken)
    {
        if (amount <= 0m)
        {
            ModelState.AddModelError(nameof(amount), "The money order amount must be positive.");
            return View();
        }

        var moneyOrder = new MoneyOrder { Amount = new Money(amount), Sender = sender, Recipient = recipient, Fee = new Money(fee), RequiresIdentityVerification = BusinessRules.RequiresIdentityVerification(new MoneyOrder { Amount = new Money(amount), Sender = sender, Recipient = recipient }) };
        if (BusinessRules.RequiresIdentityVerification(moneyOrder))
        {
            if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(maskedIdentifier))
            {
                ModelState.AddModelError(string.Empty, "High-value money orders require document type and masked identifier verification.");
                return View();
            }

            TempData["SuccessMessage"] = $"Money order approved. Identity verification recorded for {documentType}.";
            return RedirectToAction(nameof(MoneyOrder));
        }

        TempData["SuccessMessage"] = "Money order recorded successfully.";
        return RedirectToAction(nameof(MoneyOrder));
    }

    [HttpGet("IdentityVerification")]
    public async Task<IActionResult> IdentityVerification(CancellationToken cancellationToken)
    {
        var branchCode = new BranchCode(User.FindFirst("BranchCode")?.Value ?? "BR-001");
        ViewBag.BranchCode = branchCode.Value;
        ViewBag.Threshold = 1500m;
        return View();
    }

    [HttpPost("IdentityVerification")]
    [ValidateAntiForgeryToken]
    public IActionResult IdentityVerification(string documentType, string maskedIdentifier, string? customerName)
    {
        if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(maskedIdentifier))
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

        var subtotal = new Money(product.UnitPrice.Amount * quantity);
        var tax = PricingService.CalculateTaxFor(subtotal, product.TaxCategory);
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
            lineItems.Add(new TransactionLine
            {
                Description = $"Shipping - {serviceLevel}",
                ServiceCode = $"SHIP-{serviceLevel.ToUpperInvariant()}",
                Quantity = 1,
                UnitPrice = new Money(12.50m),
                TaxCategory = TaxCategory.Standard
            });
        }

        var payment = Enum.TryParse<PaymentMethod>(paymentMethod, true, out var parsedPayment) ? parsedPayment : PaymentMethod.Card;
        var transaction = new CounterTransaction
        {
            BranchCode = branchCode,
            CounterId = string.IsNullOrWhiteSpace(counterId) ? "C1" : counterId,
            ClerkId = clerkId,
            PaymentMethod = payment,
            Status = TransactionStatus.Paid,
            LineItems = lineItems,
            SubTotal = subtotal,
            TaxTotal = tax,
            Total = new Money(subtotal.Amount + tax.Amount + lineItems.Where(x => x.ServiceCode is not null).Sum(x => x.UnitPrice.Amount)),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ClosedAtUtc = DateTimeOffset.UtcNow
        };

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        stock.QuantityOnHand -= quantity;
        await _stockRepository.UpdateAsync(stock, cancellationToken);

        TempData["SuccessMessage"] = $"Transaction {transaction.Id} recorded for {product.Name}.";
        return RedirectToAction(nameof(New));
    }

    [Authorize(Policy = "BranchManagerOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(Guid transactionId, string reason, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("BranchManager"))
        {
            return Forbid();
        }

        var transaction = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            ModelState.AddModelError(nameof(reason), "A void requires a reason.");
            return RedirectToAction(nameof(HomeController.Dashboard), "Home");
        }

        transaction.Status = TransactionStatus.Voided;
        transaction.VoidReason = reason;
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        return RedirectToAction(nameof(HomeController.Dashboard), "Home");
    }
}
