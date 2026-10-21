# SKU Price API

An ASP.NET Core API that retrieves a product price from an upstream ERP service by SKU.

## Architecture

The application is a modular monolith: the API, application, domain, and infrastructure are compiled and deployed as one ASP.NET Core service. Folders and namespaces separate responsibilities and follow Clean Architecture-inspired dependency rules, with DDD domain types at the core. These are code boundaries, not separate deployable tiers or projects.

The proposed frontend and BFF interaction is documented in [doc/FE.md](doc/FE.md).

```text
Runtime request flow

+-------------+   HTTP   +-------------+   invokes   +----------------------+
| API client  | -------> | API endpoint| ---------> | Application use case |
+-------------+          +-------------+             | PriceBySkuQueryHandler|
                                                     +----------+-----------+
                                                                | calls
                                                                v
                                                     +----------------------+
                                                     | Domain port          |
                                                     | IERPClient           |
                                                     +----------^-----------+
                                                                | implemented by
                                                     +----------+-----------+
                                                     | ERPClient adapter    |
                                                     +----------+-----------+
                                                                | HTTP
                                                                v
                                                     +----------------------+
                                                     | External ERP API     |
                                                     +----------------------+

Compile-time dependencies

API ------------> Application ------------> Domain
Infrastructure ---------------------------> Domain

Infrastructure implements the domain's IERPClient port. 
Program.cs is the composition root that wires the handler and adapter to their interfaces.

Cross-cutting: exception handlers return Problem Details from the API.
```

### Request lifecycle

1. The API creates a `PriceBySkuQuery` from the route SKU and dispatches it through `IQueryHandler`.
2. `PriceBySkuQueryHandler` constructs a validated `Sku`, calls the domain `IERPClient` port, and maps the returned `PriceSnapshot` into the API response.
3. The infrastructure `ERPClient` calls `price/{sku}`, deserializes the ERP payload, and constructs validated domain values.
4. Errors are mapped as follows:
        - An ERP 404 becomes a missing result and then a domain 404.
        - Domain validation errors return 400.
        - Invalid upstream responses return 502; an open circuit returns 503; and upstream timeouts return 504.
        - Other unhandled errors return 500.

        Errors use Problem Details.

There is no database or local persistence layer. The typed HTTP client uses the standard resilience handler.

## Projects And Namespaces

The solution contains the API and its integration-test project:

| Project area | Responsibility | Representative namespaces |
| --- | --- | --- |
| `SkuPriceApi` | HTTP endpoints and dependency injection | `SkuPriceApi.Web`, top-level `Program` |
| `Application` | SKU price query and API response | `SkuPriceApi.Application.PriceBySkuQuery` |
| `Domain` | CQRS contracts, ports, exceptions, and validated price types | `SkuPriceApi.Domain.*` |
| `Infrastructure` | ERP HTTP adapter and JSON payload handling | `SkuPriceApi.Infrastructure.ExternalServices.ERPClient` |
| `SkuPriceApi.IntegrationTests` | HTTP-level tests using ASP.NET Core TestServer | `SkuPriceApi.IntegrationTests` |

The dependency direction is: application depends on domain contracts; infrastructure implements the domain's `IERPClient` port; `Program.cs` wires the implementation to the port. The API response record belongs to the application query namespace, while the ERP wire DTO belongs to the infrastructure side.

## Libraries

The projects target .NET 10 and use these direct NuGet dependencies:

| Project | Library | Version | Purpose |
| --- | --- | --- | --- |
| `SkuPriceApi` | `Microsoft.AspNetCore.OpenApi` | 10.0.13 | Generates the OpenAPI document. |
| `SkuPriceApi` | `Microsoft.Extensions.Http.Resilience` | 10.11.0 | Adds the standard resilience handler to the ERP HTTP client. |
| `SkuPriceApi` | `Swashbuckle.AspNetCore.SwaggerUI` | 10.3.0 | Provides the Swagger UI in Development. |
| `SkuPriceApi.IntegrationTests` | `Microsoft.AspNetCore.Mvc.Testing` | 10.0.13 | Hosts the API for in-memory integration tests. |
| `SkuPriceApi.IntegrationTests` | `Microsoft.NET.Test.Sdk` | 17.14.1 | Runs the test suite. |
| `SkuPriceApi.IntegrationTests` | `Moq` | 4.21.0 | Mocks the ERP HTTP handler in tests. |
| `SkuPriceApi.IntegrationTests` | `xunit` | 2.9.3 | Provides the test framework. |
| `SkuPriceApi.IntegrationTests` | `xunit.runner.visualstudio` | 3.1.4 | Integrates xUnit with the .NET test runner. |
| `SkuPriceApi.IntegrationTests` | `coverlet.collector` | 6.0.4 | Collects code coverage during test runs. |

## Domain Model

- `Sku` is a record value object. It trims and uppercases input, then requires 6-30 alphanumeric characters, hyphens, or underscores.
- `Currency` is a record value object. It stores a supported three-character code (`USD`, `EUR`, `UAH`, or `GBP`) in uppercase.
- `PriceSnapshot` is a sealed record implementing the `IValueObject` marker. It requires non-null SKU and currency values, a price greater than zero, and a non-default update timestamp.
- Record equality compares the snapshot's SKU, price, currency, and update timestamp.

```text
IValueObject (marker interface)
+-- Sku
|   +-- Value: string
+-- Currency
|   +-- Code: string
+-- PriceSnapshot
        +-- Sku: Sku
        +-- Price: decimal
        +-- Currency: Currency
        +-- LastUpdated: DateTimeOffset
```

## API

| Method | Route | Behavior |
| --- | --- | --- |
| `GET` | `/ping` | Health-style response with `pong` |
| `GET` | `/products/{sku}/price` | Returns `price`, `currency`, and `lastUpdated` |

OpenAPI and Swagger UI are enabled in the Development environment.

## Run And Test

Requires the .NET 10 SDK.

Run the API from the repository root:

```sh
dotnet run --project src/SkuPriceApi/SkuPriceApi.csproj
```

The development launch profiles use `http://localhost:5032` and `https://localhost:7160`.

Run the integration tests:

```sh
dotnet test src/SkuPrice.slnx
```

The integration tests host the API in memory and replace the ERP HTTP message handler with a mock; they do not need a live ERP service.

## Configuration

The ERP base URL is read from `ERPClient:BaseUrl` in `appsettings.json`, with `https://api.example.com` as the placeholder value. Override it per environment with an environment variable such as `ERPClient__BaseUrl`. 