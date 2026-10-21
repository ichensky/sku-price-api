
using System.Text.RegularExpressions;
using SkuPriceApi.Domain.SeedWork;
using SkuPriceApi.Domain.SeedWork.Exceptions;

namespace SkuPriceApi.Domain.SharedKernel;

public record Sku : IValueObject
{
    public static readonly int MinLength = 6;
    
    public static readonly int MaxLength = 30;
    
    public Sku(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("SKU cannot be empty.", nameof(value));
        }

        string normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length < MinLength || normalized.Length > MaxLength)
        {
            throw new DomainException($"SKU must be between {MinLength} and {MaxLength} characters.", nameof(value));
        }

        if (!SkuRegex.IsMatch(normalized))
        {
            throw new DomainException("SKU must be alphanumeric and can only contain hyphens or underscores.", nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;

    private static readonly Regex SkuRegex = new(@"^[A-Z0-9_-]+$", RegexOptions.Compiled);
}