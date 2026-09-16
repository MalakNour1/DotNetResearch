# Product API — Minimal API vs. FastEndpoints

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-SQL%20Server-CC2927?logo=microsoftsqlserver&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![FastEndpoints](https://img.shields.io/badge/FastEndpoints-REPR%20Pattern-2E8B57)](https://fast-endpoints.com/)

A side-by-side implementation of the **same Product CRUD API** built two ways on **.NET 10** — once as a plain **Minimal API**, and once with **FastEndpoints** — sharing the same EF Core model, business rules, and HTTP contract.

> No Swagger/OpenAPI UI is included by design. Endpoints are exercised via the included `.http` files (Rider / VS Code / Visual Studio HTTP Client).

---

## Table of Contents

- [Overview](#overview)
- [Repository Structure](#repository-structure)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [Configuration & Secrets](#configuration--secrets)
- [Database & Migrations](#database--migrations)
- [Running the Projects](#running-the-projects)
- [API Reference](#api-reference)
- [Architecture & Design Decisions](#architecture--design-decisions)
- [FastEndpoints Implementation Notes](#fastendpoints-implementation-notes)
- [Lessons Learned](#lessons-learned)
- [Minimal API vs. FastEndpoints vs. Controllers](#minimal-api-vs-fastendpoints-vs-controllers)

---

## Overview

Two independent ASP.NET Core Web API projects implementing identical CRUD behavior for a `Product` resource:

| Project | Description |
|---|---|
| **`MinimalApiVersion`** | Built with ASP.NET Core Minimal APIs (`MapGet`, `MapPost`, …) |
| **`VersionFastEndpoints`** | Built with [FastEndpoints](https://fast-endpoints.com/), one endpoint class per use case |

Both projects:

- Target **.NET 10**
- Share the same `Product` entity, `AppDbContext`, DTOs, and `IProductService` — all in `ProductShared`
- Expose identical routes and return identical HTTP status codes
- Enforce identical validation rules
- Ship with **zero Swagger/OpenAPI packages or middleware**

The only variable between the two is the API framework style itself.

---

## Repository Structure

```
DotNETResearch/
├── MinimalApiVersion/              # Minimal API project
│   ├── appsettings.json
│   ├── MinimalApiVersion.http
│   └── Program.cs
│
├── ProductShared/                  # Shared library — referenced by both API projects
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── Dtos/
│   │   ├── ProductRequestDto.cs
│   │   └── ProductResponseDto.cs
│   ├── Migrations/
│   ├── Models/
│   │   └── Product.cs
│   └── Services/
│       ├── IProductService.cs
│       └── ProductService.cs
│
└── VersionFastEndpoints/           # FastEndpoints project
    ├── Endpoints/
    │   └── Products/
    │       ├── CreateProductEndpoint.cs
    │       ├── GetAllProductsEndpoint.cs
    │       ├── GetProductByIdEndpoint.cs
    │       ├── UpdateProductEndpoint.cs
    │       ├── DeleteProductEndpoint.cs
    │       └── Validators/
    │           ├── CreateProductValidator.cs
    │           └── UpdateProductValidator.cs
    ├── appsettings.json
    ├── FastEndpointsTests.http
    └── Program.cs
```

---

## Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 |
| API style (Project 1) | ASP.NET Core Minimal APIs |
| API style (Project 2) | FastEndpoints (REPR pattern) |
| ORM | Entity Framework Core |
| Database | SQL Server |
| Validation | FluentValidation (via FastEndpoints) |
| Secrets (dev) | ASP.NET Core Secret Manager (User Secrets) |
| Transport security | Kestrel + ASP.NET Core HTTPS dev certificate |
| Logging | `Microsoft.Extensions.Logging` (`ILogger<T>`) |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Developer, or Express edition)
- `dotnet-ef` global tool:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

### Clone & Build

```bash
git clone https://github.com/<your-username>/DotNETResearch.git
cd DotNETResearch
dotnet restore
dotnet build
```

---

## Configuration & Secrets

The connection string is kept out of source control via **User Secrets**, set once per API project:

```bash
cd MinimalApiVersion
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=<YOUR_SERVER>;Database=ProductApiDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

```bash
cd ../VersionFastEndpoints
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=<YOUR_SERVER>;Database=ProductApiDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

`appsettings.json` contains no credentials.

---

## Database & Migrations

The `Product` entity has:

| Column | Type | Constraints |
|---|---|---|
| `Id` | `int` (PK, identity) | Auto-generated |
| `Name` | `string` | Required, max length 100, indexed |
| `Price` | `decimal(18,2)` | Required |
| `CreatedAtUtc` | `DateTime` | Set server-side, never accepted from the client |

Migrations live in `ProductShared/Migrations` and are applied from either API project:

```bash
cd MinimalApiVersion
dotnet ef migrations add InitialCreate --project ../ProductShared --startup-project .
dotnet ef database update --project ../ProductShared --startup-project .
```

---

## Running the Projects

```bash
# Minimal API — http://localhost:5045
cd MinimalApiVersion
dotnet run
```

```bash
# FastEndpoints — http://localhost:7268
cd VersionFastEndpoints
dotnet run
```

Configure different ports in each project's `launchSettings.json` so they can run side by side.

---

## API Reference

Identical contract on both projects:

| Method | Route | Success | Failure |
|---|---|---|---|
| `POST`   | `/api/products`        | `201 Created` + `Location` header + body | `400 Bad Request` (validation) |
| `GET`    | `/api/products`        | `200 OK` + array of products             | — |
| `GET`    | `/api/products/{id}`   | `200 OK` + product                       | `404 Not Found` |
| `PUT`    | `/api/products/{id}`   | `204 No Content`                         | `400` (validation) / `404` (missing) |
| `DELETE` | `/api/products/{id}`   | `204 No Content`                         | `404 Not Found` |

DTOs (in `ProductShared/Dtos`) keep the EF Core entity out of the wire contract:

```csharp
public record ProductRequestDto(string Name, decimal Price);
public record ProductResponseDto(int Id, string Name, decimal Price, DateTime CreatedAtUtc);
```

In the FastEndpoints project, `UpdateProductRequest` and `DeleteProductRequest` are local request classes that combine the route `Id` with the body fields, since FastEndpoints binds route + body into a single request DTO.

---

## Architecture & Design Decisions

### DI Lifetimes

| Service | Lifetime | Why |
|---|---|---|
| `AppDbContext` | **Scoped** | Not thread-safe; represents one unit of work per request. `Singleton` would corrupt the change tracker across requests. `Transient` would break tracking consistency within a request. |
| `IProductService` / `ProductService` | **Scoped** | Depends directly on `AppDbContext`. A service must never outlive its dependencies — making it `Singleton` would capture a `Scoped` `DbContext` (captive dependency) and fail at runtime. |

### `AsNoTracking()` on read-only queries

Used in `GetAllAsync` and `GetByIdAsync`. No change tracking is needed for data that isn't modified in the same request — this reduces memory and CPU overhead with no behavioral downside. **Not** used in `UpdateAsync` / `DeleteAsync`, which need tracking to modify entities.

### `CancellationToken` propagation

Every async EF Core call receives the token tied to `HttpContext.RequestAborted`, so a client disconnect cancels the database operation instead of letting it run to completion.

### Structured logging

`ILogger<ProductService>` logs every mutating operation and every failure using message templates (not string interpolation), so parameters become queryable fields:

```csharp
_logger.LogInformation("Created product {ProductId} with name {ProductName}", product.Id, product.Name);
_logger.LogWarning("Product {ProductId} not found", id);
```

---

## FastEndpoints Implementation Notes

`VersionFastEndpoints/Endpoints/Products` contains one endpoint class per use case, each using **constructor injection** for `IProductService`:

- `CreateProductEndpoint : Endpoint<ProductRequestDto, ProductResponseDto>`
- `GetAllProductsEndpoint : EndpointWithoutRequest<List<ProductResponseDto>>`
- `GetProductByIdEndpoint : Endpoint<GetProductByIdRequest, ProductResponseDto>`
- `UpdateProductEndpoint : Endpoint<UpdateProductRequest>`
- `DeleteProductEndpoint : Endpoint<DeleteProductRequest>`

**Startup wiring:**

```csharp
builder.Services.AddFastEndpoints();
builder.Services.AddScoped<IProductService, ProductService>();
// ...
app.UseFastEndpoints();
```

**Binding** is automatic: route parameters (`{id}`) and JSON body properties are matched onto the request DTO by name.

**Responses** go through the `Send` property:

```csharp
await Send.OkAsync(response, ct);                                  // 200
await Send.CreatedAtAsync<GetProductByIdEndpoint>(                 // 201 + Location
    routeValues: new { id = response.Id },
    responseBody: response,
    cancellation: ct);
await Send.NoContentAsync(ct);                                     // 204
await Send.NotFoundAsync(ct);                                      // 404
```

**Validation** uses `FluentValidation` validators (`CreateProductValidator`, `UpdateProductValidator`) auto-discovered by FastEndpoints. If validation fails, a `400 Bad Request` is returned automatically — no manual checks inside the endpoint.

**No Swagger** — the `FastEndpoints.Swagger` package is not installed and no Swagger middleware is configured.

---

## Lessons Learned

- **Framework naming collisions are real.** FastEndpoints silently failed to discover endpoints because the project folder was named `FastEndpointsVersion`. Renaming to `VersionFastEndpoints` resolved it.
- **Compile errors can masquerade as runtime errors.** When a build fails, Rider may run the last successful DLL, producing misleading runtime exceptions. Always check the Build tab first.
- **Shared business layers pay off.** Swapping the entire HTTP style (Minimal API → FastEndpoints) required zero changes to `ProductShared`.

---

## Minimal API vs. FastEndpoints vs. Controllers

| Criterion | Minimal APIs | FastEndpoints | Controllers |
|---|---|---|---|
| **Endpoint registration** | `app.MapPost(...)` lambdas in `Program.cs` | Endpoint classes auto-discovered via `AddFastEndpoints()` | Attribute-routed classes via `AddControllers()` |
| **Dependency injection** | Resolved in the delegate signature | Constructor injection per endpoint class | Constructor injection per controller |
| **Request binding** | Inferred from parameter types | Automatic across route / query / body into one DTO | MVC model binding |
| **Validation** | Manual | Built-in FluentValidation, auto-discovered | Data Annotations / FluentValidation via filters |
| **Filters / processors** | `IEndpointFilter` | Pre/Post processors | Action filters |
| **Response handling** | `Results.*` / `TypedResults.*` | `Send.*` methods | `ActionResult` / `IActionResult` |
| **Fewest dependencies** | ✅ Ships in the shared framework | Requires `FastEndpoints` + FluentValidation packages | Ships in ASP.NET Core, larger MVC subsystem |
| **Strongest convention** | — | ✅ One class per use case, enforced by design | Structured, but less prescriptive per action |
| **Third-party dependency risk** | — | Community-maintained package outside Microsoft's release cadence | — |

**Best fit:**

- **Small service** → Minimal APIs. Least ceremony, no extra package.
- **Vertical-slice application** → FastEndpoints. Its one-class-per-use-case design *is* the pattern.
- **Existing controller-based system** → Controllers. Team familiarity and ecosystem consistency outweigh switching costs.

This isn't a performance call — all three are fast enough. It comes down to maintainability, team familiarity, testing support, ecosystem maturity, and project size.
