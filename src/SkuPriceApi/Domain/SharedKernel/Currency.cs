using SkuPriceApi.Domain.SeedWork;
using SkuPriceApi.Domain.SeedWork.Exceptions;

namespace SkuPriceApi.Domain.SharedKernel;

public record Currency : IValueObject
{
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "UAH", "GBP"
    };

    public string Code { get; }

    public Currency(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Currency code cannot be empty.", nameof(code));
        }

        var trimmedCode = code.Trim();

        if (trimmedCode.Length != 3)
        {
            throw new DomainException("Currency code must be exactly 3 characters.", nameof(code));
        }

        Code = trimmedCode.ToUpperInvariant();

        if (!SupportedCurrencies.Contains(Code))
        {
            throw new DomainException($"Currency '{Code}' is not supported.", nameof(code));
        }
    }

    public override string ToString() => Code;
}