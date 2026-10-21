namespace SkuPriceApi.Domain.SeedWork.Exceptions;

/// <summary>
/// Represents an exception that is thrown when the ERP returns invalid data.
/// </summary>
public class ERPClientInvalidDataException : Exception
{
    public ERPClientInvalidDataException(string message)
        : base(message)
    {
    }
}