namespace ApexPost.Infrastructure;

using ApexPost.Core.Application.Services;
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

        var existingStaff = await staffRepository.GetAllAsync(cancellationToken);
        if (existingStaff.Count == 0)
        {
            foreach (var staff in SeedData.Staff)
            {
                await staffRepository.AddAsync(staff, cancellationToken);
            }
        }
        else
        {
            foreach (var staff in existingStaff)
            {
                if (string.IsNullOrWhiteSpace(staff.PasswordHash))
                {
                    var rolePassword = staff.Role switch
                    {
                        RoleType.CounterClerk => "ApexPostClerk123!",
                        RoleType.BranchManager => "ApexPostManager123!",
                        RoleType.Auditor => "ApexPostAuditor123!",
                        RoleType.Administrator => "ApexPostAdmin123!",
                        _ => "ApexPostStaff123!"
                    };

                    await staffRepository.UpdateAsync(new Staff
                    {
                        Id = staff.Id,
                        Name = staff.Name,
                        Email = staff.Email,
                        PasswordHash = PasswordHasher.Hash(rolePassword),
                        Role = staff.Role,
                        HomeBranch = staff.HomeBranch,
                        IsActive = staff.IsActive
                    }, cancellationToken);
                }
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
