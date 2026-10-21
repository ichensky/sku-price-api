using System.Text.Json;
using SkuPriceApi.Domain.Models.PriceSnapshot;
using SkuPriceApi.Domain.SeedWork.Exceptions;
using SkuPriceApi.Domain.SeedWork.Infrastructure.ExternalServices.ERPClient;
using SkuPriceApi.Domain.SharedKernel;

namespace SkuPriceApi.Infrastructure.ExternalServices.ERPClient;

public class ERPClient(HttpClient httpClient) : IERPClient
{
    public async Task<PriceSnapshot?> GetPriceBySkuAsync(Sku sku, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sku, nameof(sku));

        var url = $"price/{sku.Value}";

        using var response = await httpClient.GetAsync(url, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return CreatePriceSnapshot(sku, content);
    }

    private static PriceSnapshot CreatePriceSnapshot(Sku sku, string content)
    {
        var priceBySkuResponse = JsonSerializer.Deserialize<ERPPriceBySkuResponse>(content, ERPHttpJsonDefaults.Options)
            ?? throw new ERPClientInvalidDataException($"Failed to deserialize price for SKU '{sku.Value}'.");

        try
        {
            var currency = new Currency(priceBySkuResponse.Currency);
            var price = priceBySkuResponse.Price;

            var snapshot = new PriceSnapshot(
                sku,
                price,
                currency,
                priceBySkuResponse.LastUpdated
            );
            return snapshot;
        }
        catch (DomainException ex)
        {
            throw new ERPClientInvalidDataException($"Failed to create price snapshot for SKU '{sku.Value}'. Error: {ex.Message}");
        }
    }
}

