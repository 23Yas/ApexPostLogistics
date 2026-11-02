namespace ApexPost.Infrastructure.Persistence;

using ApexPost.Core.Domain;

/// <summary>
/// Persistence model for IdentityVerification. Deliberately NOT the domain class: the table can only hold
/// the last four characters of the identity document (MaskedLast4 is varchar(4)), whatever the domain carries.
/// See ADR-015.
/// </summary>
public sealed class IdentityVerificationRecord
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public Guid? TransactionId { get; set; }
    public BranchCode BranchCode { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string MaskedLast4 { get; set; } = string.Empty;
    public DateTimeOffset VerifiedAtUtc { get; set; }
}

public static class IdentityVerificationMapper
{
    public static IdentityVerificationRecord ToRecord(IdentityVerification domain) => new()
    {
        Id = domain.Id,
        StaffId = domain.StaffId,
        TransactionId = domain.TransactionId,
        BranchCode = domain.BranchCode,
        DocumentType = domain.DocumentType,
        IsVerified = domain.IsVerified,
        MaskedLast4 = MaskedIdentifierPolicy.LastFour(domain.MaskedIdentifier),
        VerifiedAtUtc = domain.VerifiedAtUtc
    };

    public static IdentityVerification ToDomain(IdentityVerificationRecord record) => new()
    {
        Id = record.Id,
        StaffId = record.StaffId,
        TransactionId = record.TransactionId ?? Guid.Empty,
        BranchCode = record.BranchCode,
        DocumentType = record.DocumentType,
        IsVerified = record.IsVerified,
        MaskedIdentifier = "****" + record.MaskedLast4,
        VerifiedAtUtc = record.VerifiedAtUtc
    };
}
