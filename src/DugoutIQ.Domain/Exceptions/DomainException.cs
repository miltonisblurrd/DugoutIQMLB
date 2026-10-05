namespace DugoutIQ.Domain.Exceptions;

/// <summary>
/// A baseball rule was violated. This is not an HTTP or database failure.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
