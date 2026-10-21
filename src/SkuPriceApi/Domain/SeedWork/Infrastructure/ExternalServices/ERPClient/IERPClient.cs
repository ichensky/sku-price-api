using SkuPriceApi.Domain.Models.PriceSnapshot;
using SkuPriceApi.Domain.SharedKernel;

namespace SkuPriceApi.Domain.SeedWork.Infrastructure.ExternalServices.ERPClient;

public interface IERPClient
{
    public Task<PriceSnapshot?> GetPriceBySkuAsync(Sku sku, CancellationToken cancellationToken);
}