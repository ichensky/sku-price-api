using SkuPriceApi.Domain.SeedWork.CQRS;
using SkuPriceApi.Domain.SeedWork.Exceptions;
using SkuPriceApi.Domain.SeedWork.Infrastructure.ExternalServices.ERPClient;
using SkuPriceApi.Domain.SharedKernel;

namespace SkuPriceApi.Application.PriceBySkuQuery;

public record PriceBySkuQuery(string Sku) : IQuery<PriceBySkuResponse>;

public record PriceBySkuResponse(decimal Price, string Currency, DateTimeOffset LastUpdated);

public class PriceBySkuQueryHandler(ILogger<PriceBySkuQueryHandler> logger, IERPClient erpClient) 
    : IQueryHandler<PriceBySkuQuery, PriceBySkuResponse>
{
    public async Task<PriceBySkuResponse> HandleAsync(PriceBySkuQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        
        var sku = new Sku(query.Sku);

        logger.LogInformation("Fetching price for SKU: {Sku}", sku.Value);

        var priceSnapshot = await erpClient.GetPriceBySkuAsync(sku, cancellationToken) 
            ?? throw new NotFoundDomainException($"Price for SKU '{sku.Value}' not found.", nameof(query.Sku));
            
        return new PriceBySkuResponse(priceSnapshot.Price, priceSnapshot.Currency.Code, priceSnapshot.LastUpdated);
    }
}