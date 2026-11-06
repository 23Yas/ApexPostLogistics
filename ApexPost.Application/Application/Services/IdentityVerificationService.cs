namespace ApexPost.Core.Application.Services;

using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Domain;

/// <summary>Records an identity verification. One use case, one commit: the controller never calls SaveChangesAsync.</summary>
public sealed class IdentityVerificationService
{
    private readonly IIdentityVerificationRepository _identityVerificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IdentityVerificationService(
        IIdentityVerificationRepository identityVerificationRepository,
        IUnitOfWork unitOfWork)
    {
        _identityVerificationRepository = identityVerificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IdentityVerification> RecordAsync(
        BranchCode branchCode,
        Guid staffId,
        string documentType,
        string maskedIdentifier,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentType))
        {
            throw new DomainException("Document type is required.");
        }

        if (string.IsNullOrWhiteSpace(maskedIdentifier))
        {
            throw new DomainException("A masked identifier is required.");
        }

        var verification = new IdentityVerification
        {
            StaffId = staffId,
            TransactionId = Guid.Empty,
            BranchCode = branchCode,
            DocumentType = documentType.Trim(),
            IsVerified = true,
            MaskedIdentifier = maskedIdentifier.Trim(),
            VerifiedAtUtc = DateTimeOffset.UtcNow
        };

        await _identityVerificationRepository.AddAsync(verification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return verification;
    }
}