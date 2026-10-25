namespace ApexPost.Core.Application.Services;

using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Domain;

public sealed class CounterService
{
    private readonly IBranchRepository _branchRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly IStockRepository _stockRepository;
    private readonly ITransactionRepository _transactionRepository;

    public CounterService(
        IBranchRepository branchRepository,
        IStaffRepository staffRepository,
        IStockRepository stockRepository,
        ITransactionRepository transactionRepository)
    {
        _branchRepository = branchRepository;
        _staffRepository = staffRepository;
        _stockRepository = stockRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Branch?> GetBranchAsync(BranchCode code, CancellationToken cancellationToken = default)
        => await _branchRepository.GetByCodeAsync(code, cancellationToken);

    public async Task<Staff?> GetStaffAsync(Guid staffId, CancellationToken cancellationToken = default)
        => await _staffRepository.GetByIdAsync(staffId, cancellationToken);

    public async Task<IReadOnlyCollection<StockItem>> GetStocksAsync(BranchCode branchCode, CancellationToken cancellationToken = default)
    {
        var all = await _stockRepository.GetAllAsync(cancellationToken);
        return all.Where(item => item.BranchCode == branchCode).ToList();
    }

    public async Task<IReadOnlyCollection<CounterTransaction>> GetTransactionsAsync(BranchCode branchCode, CancellationToken cancellationToken = default)
        => await _transactionRepository.GetByBranchAsync(branchCode, cancellationToken);
}
