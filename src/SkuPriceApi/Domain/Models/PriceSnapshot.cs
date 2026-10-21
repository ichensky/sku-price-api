using SkuPriceApi.Domain.SeedWork;
using SkuPriceApi.Domain.SeedWork.Exceptions;
using SkuPriceApi.Domain.SharedKernel;

namespace SkuPriceApi.Domain.Models.PriceSnapshot;

public sealed record PriceSnapshot : IValueObject
{
    public PriceSnapshot(Sku sku, decimal price, Currency currency, DateTimeOffset lastUpdated)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(currency);

        if (price <= 0)
        {
            throw new DomainException("Price must be greater than zero.", nameof(price));
        }

        if (lastUpdated == default)
        {
            throw new DomainException("LastUpdated must be a valid date.", nameof(lastUpdated));
        }

        Sku = sku;
        Price = price;
        Currency = currency;
        LastUpdated = lastUpdated;
    }

    public Sku Sku { get; } 
    
    public decimal Price { get; } 
    
    public Currency Currency { get; } 

    public DateTimeOffset LastUpdated { get; }
}
