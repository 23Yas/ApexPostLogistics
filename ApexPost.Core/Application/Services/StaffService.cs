namespace ApexPost.Core.Application.Services;

using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Domain;

public sealed class StaffService
{
    private readonly IStaffRepository _staffRepository;

    public StaffService(IStaffRepository staffRepository)
    {
        _staffRepository = staffRepository;
    }

    public Task<IReadOnlyCollection<Staff>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<Staff>>([]);

    public async Task<Staff?> AuthenticateAsync(string email, CancellationToken cancellationToken = default)
    {
        var staff = await _staffRepository.GetByEmailAsync(email, cancellationToken);
        return staff is { IsActive: true } ? staff : null;
    }
}
