namespace SkuPriceApi.Domain.SeedWork.Exceptions;

/// <summary>
/// Represents an exception that is thrown when an entity is not found.
/// </summary>
public class NotFoundDomainException : DomainException
{
    public NotFoundDomainException(string message, string? paramName) : base(message, paramName)
    {
    }
}