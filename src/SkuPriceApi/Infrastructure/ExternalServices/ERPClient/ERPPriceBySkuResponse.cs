namespace SkuPriceApi.Infrastructure.ExternalServices.ERPClient;

public sealed record ERPPriceBySkuResponse(decimal Price, string Currency, DateTimeOffset LastUpdated);