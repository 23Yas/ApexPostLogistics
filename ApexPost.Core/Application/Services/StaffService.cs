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
        => _staffRepository.GetAllAsync(cancellationToken);

    public Task<Staff?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _staffRepository.GetByIdAsync(id, cancellationToken);

    public async Task<Staff> CreateAsync(Staff staff, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(staff.Name))
        {
            throw new InvalidOperationException("Staff name is required.");
        }

        if (string.IsNullOrWhiteSpace(staff.Email))
        {
            throw new InvalidOperationException("Staff email is required.");
        }

        if (string.IsNullOrWhiteSpace(staff.PasswordHash))
        {
            throw new InvalidOperationException("A password is required for staff login.");
        }

        var existing = await _staffRepository.GetByEmailAsync(staff.Email, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("A staff record with that email already exists.");
        }

        await _staffRepository.AddAsync(staff, cancellationToken);
        return staff;
    }

    public async Task<Staff?> UpdateAsync(Staff staff, CancellationToken cancellationToken = default)
    {
        var existing = await _staffRepository.GetByIdAsync(staff.Id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(staff.Email))
        {
            var duplicate = await _staffRepository.GetByEmailAsync(staff.Email, cancellationToken);
            if (duplicate is not null && duplicate.Id != staff.Id)
            {
                throw new InvalidOperationException("Another staff member already uses that email.");
            }
        }

        var updated = new Staff
        {
            Id = staff.Id,
            Name = staff.Name,
            Email = staff.Email,
            PasswordHash = string.IsNullOrWhiteSpace(staff.PasswordHash) ? existing.PasswordHash : staff.PasswordHash,
            Role = staff.Role,
            HomeBranch = staff.HomeBranch,
            IsActive = staff.IsActive
        };

        await _staffRepository.UpdateAsync(updated, cancellationToken);
        return updated;
    }

    public async Task<bool> DeactivateAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var staff = await _staffRepository.GetByIdAsync(staffId, cancellationToken);
        if (staff is null)
        {
            return false;
        }

        var updated = new Staff
        {
            Id = staff.Id,
            Name = staff.Name,
            Email = staff.Email,
            Role = staff.Role,
            HomeBranch = staff.HomeBranch,
            IsActive = false
        };

        await _staffRepository.UpdateAsync(updated, cancellationToken);
        return true;
    }

    public async Task<Staff?> AuthenticateAsync(string email, string? password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var staff = await _staffRepository.GetByEmailAsync(email, cancellationToken);
        if (staff is not { IsActive: true })
        {
            return null;
        }

        return PasswordHasher.Verify(password, staff.PasswordHash) ? staff : null;
    }

    public Task<Staff?> AuthenticateAsync(string email, CancellationToken cancellationToken = default)
        => AuthenticateAsync(email, null, cancellationToken);
}
