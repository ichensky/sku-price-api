using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;

namespace SkuPriceApi.IntegrationTests;

public class PriceBySkuEndpointTests(SkuWebApplicationFactory factory) : IClassFixture<SkuWebApplicationFactory>
{
    private readonly Uri erpBaseUrl = new(
        factory.Services.GetRequiredService<IConfiguration>()["ERPClient:BaseUrl"]
        ?? throw new InvalidOperationException("ERPClient:BaseUrl is not configured."));
    private readonly HttpClient client = factory.CreateClient();

    private Uri ErpPriceUri(string sku) => new(erpBaseUrl, $"price/{sku.ToUpperInvariant()}");

    [Fact]
    public async Task GetPriceBySku_WhenSkuExists_ReturnsPriceDetails()
    {
        // Arrange
        var sku = "example-sku";
        var lastUpdated = DateTimeOffset.Parse("2026-10-21T12:00:00+00:00");
        var expectedJson = "{\"price\":100,\"currency\":\"USD\",\"lastUpdated\":\"2026-10-21T12:00:00+00:00\"}";

        factory.MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Get &&
                    request.RequestUri == ErpPriceUri(sku)),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedJson, System.Text.Encoding.UTF8, "application/json")
            });

        // Act
        var response = await client.GetAsync($"/products/{sku}/price");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var content = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal(100, content.RootElement.GetProperty("price").GetDecimal());
        Assert.Equal("USD", content.RootElement.GetProperty("currency").GetString());
        Assert.Equal(lastUpdated, content.RootElement.GetProperty("lastUpdated").GetDateTimeOffset());
    }

    [Fact]
    public async Task GetPriceBySku_WhenSkuIsUnknown_ReturnsNotFound()
    {
        // Arrange
        var sku = "unknown-sku";

        factory.MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Get &&
                    request.RequestUri == ErpPriceUri(sku)),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var response = await client.GetAsync($"/products/{sku}/price");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task GetPriceBySku_WhenSkuIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var sku = "1";

        // Act
        var response = await client.GetAsync($"/products/{sku}/price");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var problemDetails = System.Text.Json.JsonDocument.Parse(content);
        Assert.Equal("Domain Error", problemDetails.RootElement.GetProperty("title").GetString());
        Assert.Equal(400, problemDetails.RootElement.GetProperty("status").GetInt32());
        Assert.Equal( "SKU must be between 6 and 30 characters.",
                problemDetails.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task GetPriceBySku_WhenErpTimesOut_ReturnsGatewayTimeout()
    {
        // Arrange
        var sku = "timeout";
        factory.MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Get &&
                    request.RequestUri == ErpPriceUri(sku)),
                ItExpr.IsAny<CancellationToken>()
            )
        .Returns(async (HttpRequestMessage _, CancellationToken cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Act
        var response = await client.GetAsync($"/products/{sku}/price");

        // Assert
        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var problemDetails = System.Text.Json.JsonDocument.Parse(content);
        Assert.Equal("Gateway Timeout", problemDetails.RootElement.GetProperty("title").GetString());
        Assert.Equal(504, problemDetails.RootElement.GetProperty("status").GetInt32());
        Assert.Equal( "The upstream service timed out.",
                problemDetails.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task GetPriceBySku_WhenErpReturnsUnsupportedCurrency_ReturnsBadGateway()
    {
        // Arrange
        var sku = "example-sku";
        var lastUpdated = DateTimeOffset.Parse("2026-10-21T12:00:00+00:00");
        var erpJson = "{\"price\":100,\"currency\":\"XXX\",\"lastUpdated\":\"2026-10-21T12:00:00+00:00\"}";

        factory.MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Get &&
                    request.RequestUri == ErpPriceUri(sku)),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(erpJson, System.Text.Encoding.UTF8, "application/json")
            });

        // Act
        var response = await client.GetAsync($"/products/{sku}/price");

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var problemDetails = System.Text.Json.JsonDocument.Parse(content);
        Assert.Equal("Bad Gateway", problemDetails.RootElement.GetProperty("title").GetString());
        Assert.Equal(502, problemDetails.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("The upstream service returned an invalid response.",
                problemDetails.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task GetPriceBySku_WhenUnexpectedExceptionOccurs_ReturnsInternalServerError()
    {
        // Arrange
        var sku = "unexpected-error";
        factory.MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Get &&
                    request.RequestUri == ErpPriceUri(sku)),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new InvalidOperationException("Unexpected test failure."));

        // Act
        var response = await client.GetAsync($"/products/{sku}/price");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var problemDetails = System.Text.Json.JsonDocument.Parse(content);
        Assert.Equal("Server Error", problemDetails.RootElement.GetProperty("title").GetString());
        Assert.Equal(500, problemDetails.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred.",
            problemDetails.RootElement.GetProperty("detail").GetString());
    }
}