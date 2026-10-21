using System.Text.Json;

namespace SkuPriceApi.Domain.SeedWork.Infrastructure.ExternalServices.ERPClient;

public static class ERPHttpJsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
}