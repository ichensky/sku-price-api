using System;

namespace SkuPriceApi.Domain.SeedWork.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message, string? paramName) : base(message)
    {
        ParamName = paramName;
    }

    public string? ParamName { get; }
}