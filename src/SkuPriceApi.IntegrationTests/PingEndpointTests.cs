using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SkuPriceApi.IntegrationTests;

public class PingEndpointTests(SkuWebApplicationFactory factory) : IClassFixture<SkuWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Get_Root_Endpoint_Returns_Success()
    {
        // Act 
        var response = await client.GetAsync("/ping");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("pong", content);
    }
}