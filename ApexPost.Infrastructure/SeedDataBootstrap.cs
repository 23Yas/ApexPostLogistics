namespace ApexPost.Infrastructure;

using ApexPost.Core.Domain;

public static class SeedDataBootstrap
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var root = AppContext.BaseDirectory;
        var branchRepository = new BranchRepository(Path.Combine(root, "data", "branches.json"));
        var staffRepository = new StaffRepository(Path.Combine(root, "data", "staff.json"));
        var productRepository = new ProductRepository(Path.Combine(root, "data", "products.json"));
        var stockRepository = new StockRepository(Path.Combine(root, "data", "stock.json"));
        var transactionRepository = new TransactionRepository(Path.Combine(root, "data", "transactions.json"));

        if ((await branchRepository.GetAllAsync(cancellationToken)).Count == 0)
        {
            foreach (var branch in SeedData.Branches)
            {
                await branchRepository.AddAsync(branch, cancellationToken);
            }
        }

        if ((await staffRepository.GetAllAsync(cancellationToken)).Count == 0)
        {
            foreach (var staff in SeedData.Staff)
            {
                await staffRepository.AddAsync(staff, cancellationToken);
            }
        }

        if ((await productRepository.GetAllAsync(cancellationToken)).Count == 0)
        {
            foreach (var product in SeedData.Products)
            {
                await productRepository.AddAsync(product, cancellationToken);
            }
        }

        if ((await stockRepository.GetAllAsync(cancellationToken)).Count == 0)
        {
            foreach (var stock in SeedData.Stock)
            {
                await stockRepository.AddAsync(stock, cancellationToken);
            }
        }

        if ((await transactionRepository.GetAllAsync(cancellationToken)).Count == 0)
        {
            foreach (var transaction in SeedData.Transactions)
            {
                await transactionRepository.AddAsync(transaction, cancellationToken);
            }
        }
    }
}
