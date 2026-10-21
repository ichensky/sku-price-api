using SkuPriceApi.Domain.SeedWork.Infrastructure.ExternalServices.ERPClient;
using SkuPriceApi.Infrastructure.ExternalServices.ERPClient;
using SkuPriceApi.Web;
using SkuPriceApi.Application.PriceBySkuQuery;
using SkuPriceApi.Domain.SeedWork.CQRS;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var erpBaseUrl = builder.Configuration["ERPClient:BaseUrl"]
    ?? throw new InvalidOperationException("Configuration value 'ERPClient:BaseUrl' is required.");

builder.Host.UseDefaultServiceProvider((context, options) => { 
    options.ValidateScopes = true; 
    options.ValidateOnBuild = true;
});

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHttpClient<IERPClient, ERPClient>(
        (httpClient) =>
        {
            httpClient.BaseAddress = new Uri(erpBaseUrl, UriKind.Absolute);
        }
    )
    .AddStandardResilienceHandler();

builder.Services.AddScoped<IQueryHandler<PriceBySkuQuery, PriceBySkuResponse>, PriceBySkuQueryHandler>();


var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Sku Price API v1");
    });
}

app.UseHttpsRedirection();

app.MapGet("/ping", ()=> "pong");

app.MapGet("/products/{sku}/price", async (
    [FromRoute] string sku,
    [FromServices] IQueryHandler<PriceBySkuQuery, PriceBySkuResponse> queryHandler,
    CancellationToken cancellationToken) =>
{
    var query = new PriceBySkuQuery(sku);

    var response = await queryHandler.HandleAsync(query, cancellationToken);

    return response;
})
.WithName("GetPriceBySku");

app.Run();

