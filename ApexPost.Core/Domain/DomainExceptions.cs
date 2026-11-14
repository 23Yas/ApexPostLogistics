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

/// <summary>
/// Base for domain failures that map to a specific HTTP status. The API's
/// GlobalExceptionHandler turns these into ProblemDetails responses, so
/// controllers stay thin and never build error bodies by hand.
/// </summary>
public abstract class HttpDomainException : Exception
{
    public abstract int StatusCode { get; }
    public abstract string Title { get; }

    protected HttpDomainException(string message) : base(message)
    {
    }
}

/// <summary>Raised when a requested entity does not exist (HTTP 404).</summary>
public sealed class NotFoundException : HttpDomainException
{
    public override int StatusCode => 404;
    public override string Title => "Not Found";

    public NotFoundException(string message) : base(message)
    {
    }
}

/// <summary>Raised when a request conflicts with the current state (HTTP 409).</summary>
public sealed class ConflictException : HttpDomainException
{
    public override int StatusCode => 409;
    public override string Title => "Conflict";

    public ConflictException(string message) : base(message)
    {
    }
}

/// <summary>Raised when a request is malformed or violates a rule (HTTP 400).</summary>
public sealed class BadRequestException : HttpDomainException
{
    public override int StatusCode => 400;
    public override string Title => "Bad Request";

    public BadRequestException(string message) : base(message)
    {
    }
}
