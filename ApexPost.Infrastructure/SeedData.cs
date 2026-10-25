namespace ApexPost.Infrastructure;

using ApexPost.Core.Domain;

public static class SeedData
{
    public static IReadOnlyCollection<Branch> Branches { get; } =
    [
        new Branch { Code = new BranchCode("BR-001"), Name = "Birmingham Hub", Region = "Midlands", OpeningHours = "08:00-18:00", IsActive = true, Counters = ["C1", "C2", "C3"] },
        new Branch { Code = new BranchCode("BR-002"), Name = "Manchester North", Region = "North", OpeningHours = "09:00-17:00", IsActive = true, Counters = ["C1", "C2"] },
        new Branch { Code = new BranchCode("BR-003"), Name = "Leeds Central", Region = "North", OpeningHours = "08:30-18:30", IsActive = true, Counters = ["C1", "C2", "C4"] },
        new Branch { Code = new BranchCode("BR-004"), Name = "London City", Region = "South", OpeningHours = "07:30-19:00", IsActive = true, Counters = ["C1", "C2", "C3"] },
        new Branch { Code = new BranchCode("BR-005"), Name = "Cardiff South", Region = "South West", OpeningHours = "08:00-17:30", IsActive = true, Counters = ["C1", "C2"] }
    ];

    public static IReadOnlyCollection<Staff> Staff { get; } =
    [
        new Staff { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Ava Reed", Email = "ava.reed@apexpost.local", Role = RoleType.CounterClerk, HomeBranch = new BranchCode("BR-001"), IsActive = true },
        new Staff { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Ben Cole", Email = "ben.cole@apexpost.local", Role = RoleType.CounterClerk, HomeBranch = new BranchCode("BR-001"), IsActive = true },
        new Staff { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Chloe Moss", Email = "chloe.moss@apexpost.local", Role = RoleType.BranchManager, HomeBranch = new BranchCode("BR-001"), IsActive = true },
        new Staff { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Daniel Price", Email = "daniel.price@apexpost.local", Role = RoleType.Auditor, HomeBranch = new BranchCode("BR-002"), IsActive = true },
        new Staff { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Ella North", Email = "ella.north@apexpost.local", Role = RoleType.Administrator, HomeBranch = new BranchCode("BR-004"), IsActive = true },
        new Staff { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Frank Osei", Email = "frank.osei@apexpost.local", Role = RoleType.CounterClerk, HomeBranch = new BranchCode("BR-002"), IsActive = true },
        new Staff { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "Grace Hill", Email = "grace.hill@apexpost.local", Role = RoleType.CounterClerk, HomeBranch = new BranchCode("BR-003"), IsActive = true },
        new Staff { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "Harper Finch", Email = "harper.finch@apexpost.local", Role = RoleType.BranchManager, HomeBranch = new BranchCode("BR-002"), IsActive = true },
        new Staff { Id = Guid.Parse("99999999-9999-9999-9999-999999999999"), Name = "Imran Khan", Email = "imran.khan@apexpost.local", Role = RoleType.CounterClerk, HomeBranch = new BranchCode("BR-004"), IsActive = true },
        new Staff { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name = "Jasmine Lee", Email = "jasmine.lee@apexpost.local", Role = RoleType.CounterClerk, HomeBranch = new BranchCode("BR-005"), IsActive = true },
        new Staff { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Name = "Kai Turner", Email = "kai.turner@apexpost.local", Role = RoleType.BranchManager, HomeBranch = new BranchCode("BR-005"), IsActive = true },
        new Staff { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Name = "Lila Brooks", Email = "lila.brooks@apexpost.local", Role = RoleType.Auditor, HomeBranch = new BranchCode("BR-003"), IsActive = true }
    ];

    public static IReadOnlyCollection<Product> Products { get; } =
    [
        new Product { Sku = new Sku("SKU-1001"), Name = "Bubble Wrap Roll", Category = ProductCategory.Packaging, UnitPrice = new Money(8.50m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1002"), Name = "Packing Tape", Category = ProductCategory.Packaging, UnitPrice = new Money(6.25m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1003"), Name = "Mailer Box Small", Category = ProductCategory.Packaging, UnitPrice = new Money(12.00m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1004"), Name = "A4 Printer Paper", Category = ProductCategory.OfficeSupplies, UnitPrice = new Money(5.90m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1005"), Name = "Ballpoint Pen", Category = ProductCategory.Stationery, UnitPrice = new Money(1.75m), TaxCategory = TaxCategory.Reduced },
        new Product { Sku = new Sku("SKU-1006"), Name = "Sticky Notes", Category = ProductCategory.Stationery, UnitPrice = new Money(2.20m), TaxCategory = TaxCategory.Reduced },
        new Product { Sku = new Sku("SKU-1007"), Name = "Desk Calendar", Category = ProductCategory.OfficeSupplies, UnitPrice = new Money(11.99m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1008"), Name = "Letter Envelope Pack", Category = ProductCategory.Packaging, UnitPrice = new Money(3.50m), TaxCategory = TaxCategory.Reduced },
        new Product { Sku = new Sku("SKU-1009"), Name = "Stapler", Category = ProductCategory.OfficeSupplies, UnitPrice = new Money(9.20m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1010"), Name = "Notebook A5", Category = ProductCategory.Stationery, UnitPrice = new Money(4.30m), TaxCategory = TaxCategory.Reduced },
        new Product { Sku = new Sku("SKU-1011"), Name = "Plastic Folder", Category = ProductCategory.OfficeSupplies, UnitPrice = new Money(2.80m), TaxCategory = TaxCategory.Reduced },
        new Product { Sku = new Sku("SKU-1012"), Name = "Shipping Label Pack", Category = ProductCategory.Packaging, UnitPrice = new Money(7.00m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1013"), Name = "Document Wallet", Category = ProductCategory.Packaging, UnitPrice = new Money(4.75m), TaxCategory = TaxCategory.Reduced },
        new Product { Sku = new Sku("SKU-1014"), Name = "Padded Envelope", Category = ProductCategory.Packaging, UnitPrice = new Money(5.40m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1015"), Name = "Courier Bag", Category = ProductCategory.Packaging, UnitPrice = new Money(6.90m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1016"), Name = "Push Pins", Category = ProductCategory.OfficeSupplies, UnitPrice = new Money(2.10m), TaxCategory = TaxCategory.Reduced },
        new Product { Sku = new Sku("SKU-1017"), Name = "Filing Tray", Category = ProductCategory.OfficeSupplies, UnitPrice = new Money(13.60m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1018"), Name = "Highlighter Pack", Category = ProductCategory.Stationery, UnitPrice = new Money(3.95m), TaxCategory = TaxCategory.Reduced },
        new Product { Sku = new Sku("SKU-1019"), Name = "Desk Organiser", Category = ProductCategory.OfficeSupplies, UnitPrice = new Money(18.20m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1020"), Name = "Postal Scale", Category = ProductCategory.OfficeSupplies, UnitPrice = new Money(29.50m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1021"), Name = "Post-It Notes", Category = ProductCategory.Stationery, UnitPrice = new Money(2.60m), TaxCategory = TaxCategory.Reduced },
        new Product { Sku = new Sku("SKU-1022"), Name = "Premium Envelope", Category = ProductCategory.Packaging, UnitPrice = new Money(7.40m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1023"), Name = "Delivery Tape", Category = ProductCategory.Packaging, UnitPrice = new Money(9.10m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1024"), Name = "Laser Printer Toner", Category = ProductCategory.OfficeSupplies, UnitPrice = new Money(34.00m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1025"), Name = "Stylus Pen", Category = ProductCategory.Stationery, UnitPrice = new Money(4.50m), TaxCategory = TaxCategory.Reduced },
        new Product { Sku = new Sku("SKU-1026"), Name = "Sealing Labels", Category = ProductCategory.Packaging, UnitPrice = new Money(5.80m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1027"), Name = "Index Divider Set", Category = ProductCategory.OfficeSupplies, UnitPrice = new Money(8.90m), TaxCategory = TaxCategory.Reduced },
        new Product { Sku = new Sku("SKU-1028"), Name = "Quarter-Fold Brochure", Category = ProductCategory.Packaging, UnitPrice = new Money(3.30m), TaxCategory = TaxCategory.Reduced },
        new Product { Sku = new Sku("SKU-1029"), Name = "Receipt Book", Category = ProductCategory.OfficeSupplies, UnitPrice = new Money(6.10m), TaxCategory = TaxCategory.Standard },
        new Product { Sku = new Sku("SKU-1030"), Name = "Identity Verification Pad", Category = ProductCategory.OfficeSupplies, UnitPrice = new Money(12.80m), TaxCategory = TaxCategory.Standard }
    ];

    public static IReadOnlyCollection<StockItem> Stock { get; } =
    [
        new StockItem { BranchCode = new BranchCode("BR-001"), ProductSku = new Sku("SKU-1001"), QuantityOnHand = 18, ReorderThreshold = 5 },
        new StockItem { BranchCode = new BranchCode("BR-001"), ProductSku = new Sku("SKU-1002"), QuantityOnHand = 12, ReorderThreshold = 4 },
        new StockItem { BranchCode = new BranchCode("BR-001"), ProductSku = new Sku("SKU-1003"), QuantityOnHand = 7, ReorderThreshold = 3 },
        new StockItem { BranchCode = new BranchCode("BR-001"), ProductSku = new Sku("SKU-1005"), QuantityOnHand = 9, ReorderThreshold = 4 },
        new StockItem { BranchCode = new BranchCode("BR-002"), ProductSku = new Sku("SKU-1001"), QuantityOnHand = 13, ReorderThreshold = 5 },
        new StockItem { BranchCode = new BranchCode("BR-002"), ProductSku = new Sku("SKU-1004"), QuantityOnHand = 10, ReorderThreshold = 5 },
        new StockItem { BranchCode = new BranchCode("BR-002"), ProductSku = new Sku("SKU-1010"), QuantityOnHand = 6, ReorderThreshold = 5 },
        new StockItem { BranchCode = new BranchCode("BR-003"), ProductSku = new Sku("SKU-1006"), QuantityOnHand = 8, ReorderThreshold = 4 },
        new StockItem { BranchCode = new BranchCode("BR-003"), ProductSku = new Sku("SKU-1008"), QuantityOnHand = 4, ReorderThreshold = 4 },
        new StockItem { BranchCode = new BranchCode("BR-003"), ProductSku = new Sku("SKU-1012"), QuantityOnHand = 5, ReorderThreshold = 4 },
        new StockItem { BranchCode = new BranchCode("BR-004"), ProductSku = new Sku("SKU-1002"), QuantityOnHand = 15, ReorderThreshold = 6 },
        new StockItem { BranchCode = new BranchCode("BR-004"), ProductSku = new Sku("SKU-1007"), QuantityOnHand = 7, ReorderThreshold = 3 },
        new StockItem { BranchCode = new BranchCode("BR-005"), ProductSku = new Sku("SKU-1009"), QuantityOnHand = 9, ReorderThreshold = 4 },
        new StockItem { BranchCode = new BranchCode("BR-005"), ProductSku = new Sku("SKU-1011"), QuantityOnHand = 2, ReorderThreshold = 4 }
    ];

    public static IReadOnlyCollection<CounterTransaction> Transactions { get; } = GenerateTransactions();

    private static IReadOnlyCollection<CounterTransaction> GenerateTransactions()
    {
        var results = new List<CounterTransaction>();
        var branchCodes = Branches.Select(branch => branch.Code).ToArray();
        var branchStaff = Staff
            .Where(staff => staff.Role == RoleType.CounterClerk || staff.Role == RoleType.BranchManager)
            .GroupBy(staff => staff.HomeBranch)
            .ToDictionary(group => group.Key, group => group.Select(staff => staff.Id).ToArray());

        for (var i = 0; i < 40; i++)
        {
            var branchCode = branchCodes[i % branchCodes.Length];
            var clerkIds = branchStaff.TryGetValue(branchCode, out var ids) ? ids : [Guid.Empty];
            var clerkId = clerkIds[i % clerkIds.Length];
            var subtotal = 35m + (i * 13.5m % 240m);
            var status = i % 7 == 0 ? TransactionStatus.Voided : TransactionStatus.Paid;
            var paymentMethod = (PaymentMethod)(i % Enum.GetValues<PaymentMethod>().Length);
            var serviceCode = i % 3 == 0 ? "PARCEL-STANDARD" : i % 3 == 1 ? "PARCEL-EXPRESS" : "LETTER-REGISTERED";

            results.Add(new CounterTransaction
            {
                BranchCode = branchCode,
                CounterId = $"C{(i % 4) + 1}",
                ClerkId = clerkId,
                PaymentMethod = paymentMethod,
                Status = status,
                LineItems =
                [
                    new TransactionLine { Description = "Retail basket", ProductSku = new Sku($"SKU-{1000 + (i % 24)}"), Quantity = 1 + (i % 3), UnitPrice = new Money(subtotal / 3), TaxCategory = TaxCategory.Standard },
                    new TransactionLine { Description = "Shipping service", ServiceCode = serviceCode, Quantity = 1, UnitPrice = new Money(12.50m + (i % 5) * 2.5m), TaxCategory = TaxCategory.Standard }
                ],
                SubTotal = new Money(subtotal),
                TaxTotal = new Money(subtotal * 0.15m),
                Total = new Money(subtotal * 1.15m),
                ClosedAtUtc = DateTimeOffset.UtcNow.AddDays(-(i % 12) - 1),
                VoidReason = status == TransactionStatus.Voided ? "Customer requested cancellation" : null
            });
        }

        return results;
    }

    private static CounterTransaction CreateSampleTransaction(BranchCode branchCode, string counterId, Guid clerkId, decimal subtotal)
    {
        return new CounterTransaction
        {
            BranchCode = branchCode,
            CounterId = counterId,
            ClerkId = clerkId,
            PaymentMethod = PaymentMethod.Card,
            Status = TransactionStatus.Paid,
            LineItems =
            [
                new TransactionLine { Description = "Packing tape", ProductSku = new Sku("SKU-1002"), Quantity = 2, UnitPrice = new Money(6.25m), TaxCategory = TaxCategory.Standard },
                new TransactionLine { Description = "Express parcel", ServiceCode = "PARCEL-EXPRESS", Quantity = 1, UnitPrice = new Money(subtotal / 2), TaxCategory = TaxCategory.Standard }
            ],
            SubTotal = new Money(subtotal),
            TaxTotal = new Money(subtotal * 0.15m),
            Total = new Money(subtotal * 1.15m),
            ClosedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }
}
