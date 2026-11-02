namespace ApexPost.Core.Domain;

/// <summary>
/// Raised when a business rule is broken (insufficient stock, void without reason, ...).
/// Derives from InvalidOperationException so existing catch blocks (StaffController) keep working.
/// The web layer maps this to HTTP 400 Problem Details.
/// </summary>
public class DomainException : InvalidOperationException
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
