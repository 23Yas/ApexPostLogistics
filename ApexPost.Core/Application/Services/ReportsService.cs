namespace ApexPost.Core.Application.Services;

using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Domain;

public sealed class ReportsService
{
    private readonly IBranchRepository _branchRepository;
    private readonly IProductRepository _productRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IStockRepository _stockRepository;

    public ReportsService(
        IBranchRepository branchRepository,
        IProductRepository productRepository,
        ITransactionRepository transactionRepository,
        IStockRepository stockRepository)
    {
        _branchRepository = branchRepository;
        _productRepository = productRepository;
        _transactionRepository = transactionRepository;
        _stockRepository = stockRepository;
    }

    public async Task<IReadOnlyCollection<Branch>> GetBranchesAsync(CancellationToken cancellationToken = default)
        => await _branchRepository.GetAllAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
        => await _productRepository.GetAllAsync(cancellationToken);

    public async Task<IReadOnlyCollection<StockItem>> GetLowStockAsync(BranchCode branchCode, CancellationToken cancellationToken = default)
        => await _stockRepository.GetLowStockAsync(branchCode, 5, cancellationToken);

    public async Task<IReadOnlyCollection<CounterTransaction>> GetTransactionsAsync(BranchCode branchCode, CancellationToken cancellationToken = default)
        => await _transactionRepository.GetByBranchAsync(branchCode, cancellationToken);
}
