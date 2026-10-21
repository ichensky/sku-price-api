using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Moq;
using SkuPriceApi.Domain.SeedWork.Infrastructure.ExternalServices.ERPClient;
using SkuPriceApi.Infrastructure.ExternalServices.ERPClient;

namespace SkuPriceApi.IntegrationTests;

public class SkuWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<HttpMessageHandler> MockHttpMessageHandler { get; } = new Mock<HttpMessageHandler>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // To speed up tests, configure the resilience options with minimal timeouts and retries.
            services.Configure<HttpStandardResilienceOptions>("IERPClient-standard", options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromMilliseconds(100);
                    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(1);
            });

            services.AddHttpClient<IERPClient, ERPClient>()
                .ConfigurePrimaryHttpMessageHandler(() => MockHttpMessageHandler.Object);
        });
    }
}