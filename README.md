# Product API — Minimal API vs. FastEndpoints

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-SQL%20Server-CC2927?logo=microsoftsqlserver&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![FastEndpoints](https://img.shields.io/badge/FastEndpoints-REPR%20Pattern-2E8B57)](https://fast-endpoints.com/)
[![Postman](https://img.shields.io/badge/Tested%20with-Postman-FF6C37?logo=postman&logoColor=white)](https://www.postman.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)

A side-by-side implementation of the **same Product CRUD API** built two ways on **.NET 10** — once as a plain **Minimal API**, and once with **FastEndpoints** — sharing the same EF Core model, business rules, and HTTP contract. The goal is to compare the two API styles honestly, using identical behavior as the control variable.

> No Swagger/OpenAPI UI is included in either project by design. All endpoints were designed and verified manually with a Postman collection (included in `/postman`).

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
- [Testing with Postman](#testing-with-postman)
- [HTTPS & Local Certificates](#https--local-certificates)
- [Minimal API vs. FastEndpoints vs. Controllers](#minimal-api-vs-fastendpoints-vs-controllers)
- [Final Recommendation](#final-recommendation)

---

## Overview

This repository contains **two independent ASP.NET Core Web API projects** that implement identical CRUD behavior for a `Product` resource:

| | Description |
|---|---|
| **`ProductApi.MinimalApi`** | Built with ASP.NET Core Minimal APIs (`MapGet`, `MapPost`, etc.) |
| **`ProductApi.FastEndpoints`** | Built with [FastEndpoints](https://fast-endpoints.com/), one endpoint class per use case |

Both projects:
- Target **.NET 10**
- Use the **same** `Product` entity, EF Core model, and SQL Server database provider
- Expose the **same** routes and return the **same** HTTP status codes
- Enforce the **same** validation rules
- Run over **HTTPS** with the ASP.NET Core development certificate
- Ship with **zero Swagger/OpenAPI packages or middleware** — Postman is the API client of record

The point of keeping everything else identical is to isolate exactly one variable: *the API framework style itself*.

---

## Repository Structure

```
├── src/
│   ├── ProductApi.MinimalApi/
│   │   ├── Data/
│   │   │   └── AppDbContext.cs
│   │   ├── Endpoints/
│   │   │   └── ProductEndpoints.cs
│   │   ├── Models/
│   │   │   ├── Product.cs
│   │   │   └── Dtos/
│   │   │       ├── CreateProductRequest.cs
│   │   │       ├── UpdateProductRequest.cs
│   │   │       └── ProductResponse.cs
│   │   ├── Services/
│   │   │   ├── IProductService.cs
│   │   │   └── ProductService.cs
│   │   ├── Migrations/
│   │   ├── appsettings.json
│   │   └── Program.cs
│   │
│   └── ProductApi.FastEndpoints/
│       ├── Data/
│       │   └── AppDbContext.cs
│       ├── Features/
│       │   └── Products/
│       │       ├── CreateProductEndpoint.cs
│       │       ├── GetProductEndpoint.cs
│       │       ├── GetAllProductsEndpoint.cs
│       │       ├── UpdateProductEndpoint.cs
│       │       ├── DeleteProductEndpoint.cs
│       │       └── CreateProductValidator.cs
│       ├── Models/
│       │   ├── Product.cs
│       │   └── Dtos/
│       ├── Services/
│       │   ├── IProductService.cs
│       │   └── ProductService.cs
│       ├── Migrations/
│       ├── appsettings.json
│       └── Program.cs
│
├── postman/
│   ├── ProductApi.postman_collection.json
│   └── ProductApi.postman_environment.json
│
├── docs/
│   └── research/                  # Written research answers (Parts 1–5, 8, and FastEndpoints)
│
└── README.md
```

> Folder names above reflect the intended project layout — adjust to match your actual solution/project names before pushing if they differ.

---

## Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 |
| API style (Project 1) | ASP.NET Core Minimal APIs |
| API style (Project 2) | [FastEndpoints](https://fast-endpoints.com/) (REPR pattern) |
| ORM | Entity Framework Core |
| Database | SQL Server (local instance) |
| Validation | FluentValidation (via FastEndpoints) |
| Secrets (dev) | ASP.NET Core Secret Manager (User Secrets) |
| Transport security | Kestrel + ASP.NET Core HTTPS development certificate |
| API testing | Postman (collection + environment exported) |
| Logging | `Microsoft.Extensions.Logging` (`ILogger<T>`) |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Developer, or Express edition)
- [Postman](https://www.postman.com/downloads/) for testing
- (Optional) `dotnet-ef` global tool for migrations:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

### Clone

```bash
git clone https://github.com/<your-username>/<your-repo>.git
cd <your-repo>
```

### Restore & Build

```bash
dotnet restore
dotnet build
```

---

## Configuration & Secrets

The local SQL Server connection string is **never committed to source control**. It's stored per-project using the .NET **Secret Manager** (User Secrets), which keeps it outside the repository entirely.

Run this once per project (from inside each project's folder):

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=ProductApiDb;Trusted_Connection=True;MultipleActiveResultSets=true"
```

`appsettings.json` only contains non-sensitive defaults (logging levels, allowed hosts) — no credentials, ever.

---

## Database & Migrations

Both projects share the same `Product` entity and constraints:

| Column | Type | Constraints |
|---|---|---|
| `Id` | `int` (PK, identity) | Auto-generated |
| `Name` | `string` | Required, max length enforced |
| `Price` | `decimal` | Required, must be greater than 0 |
| `CreatedAtUtc` | `DateTime` | Set server-side on creation, never accepted from the client |

Create and apply the initial migration for each project:

```bash
cd src/ProductApi.MinimalApi
dotnet ef migrations add InitialCreate
dotnet ef database update
```

```bash
cd src/ProductApi.FastEndpoints
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Running the Projects

```bash
# Minimal API — https://localhost:5001
cd src/ProductApi.MinimalApi
dotnet run

# FastEndpoints — https://localhost:5101 (or as configured in launchSettings.json)
cd src/ProductApi.FastEndpoints
dotnet run
```

Both projects run over HTTPS out of the box using the trusted local development certificate (see [HTTPS & Local Certificates](#https--local-certificates)).

---

## API Reference

Both projects expose an **identical** contract for the `Product` resource.

| Method | Route | Success | Failure |
|---|---|---|---|
| `POST` | `/products` | `201 Created` + `Location` header + body | `400 Bad Request` (validation) |
| `GET` | `/products` | `200 OK` + array of products | — |
| `GET` | `/products/{id}` | `200 OK` + product | `404 Not Found` |
| `PUT` | `/products/{id}` | `200 OK` + updated product | `400` (validation) / `404` (missing) |
| `DELETE` | `/products/{id}` | `204 No Content` | `404 Not Found` |

**Request/Response DTOs** — the EF Core entity is never used as a request or response model directly:

```csharp
public record CreateProductRequest(string Name, decimal Price);
public record UpdateProductRequest(string Name, decimal Price);
public record ProductResponse(int Id, string Name, decimal Price, DateTime CreatedAtUtc);
```

This keeps the wire contract stable even if the database schema evolves, and prevents a client from ever setting server-owned fields like `CreatedAtUtc`.

---

## Architecture & Design Decisions

### DI Lifetimes

| Service | Lifetime | Why |
|---|---|---|
| `AppDbContext` | **Scoped** | EF Core's `DbContext` is not thread-safe and is designed to represent a single unit of work; scoping it to the request means every operation within that request shares one context and one change tracker, and it's disposed cleanly at the end of the request. |
| `IProductService` / `ProductService` | **Scoped** | It depends directly on `AppDbContext`. A service can never safely outlive a dependency with a shorter lifetime — making it `Singleton` would attempt to hold onto a `Scoped` `DbContext` instance across requests, which throws at runtime (captive dependency). `Scoped` keeps the lifetimes aligned. |

### Why `AsNoTracking()` on read-only queries

`GET` endpoints (list and by-id) never modify data, so there's no need for EF Core's change tracker to keep a snapshot of the returned entities for later comparison. `AsNoTracking()` skips that bookkeeping, which reduces memory usage and measurably speeds up read-only queries — with no behavioral downside, since nothing is ever updated from these results.

### `CancellationToken` propagation

Every asynchronous EF Core call (`ToListAsync`, `FindAsync`, `SaveChangesAsync`, etc.) receives the `CancellationToken` supplied by ASP.NET Core for the current request, so that if a client disconnects or a request times out, the database operation is cancelled instead of running to completion for no one.

### Structured logging

`ILogger<ProductService>` is injected into the service layer (not the endpoint layer), and every mutating operation logs a structured event using message templates rather than string interpolation, e.g.:

```csharp
_logger.LogInformation("Product {ProductId} created with price {Price}", product.Id, product.Price);
_logger.LogWarning("Product {ProductId} not found for update", id);
_logger.LogError(ex, "Failed to delete product {ProductId}", id);
```

This keeps `{ProductId}` and `{Price}` as separate, queryable properties in the log event rather than baked into an opaque string.

---

## FastEndpoints Implementation Notes

The FastEndpoints project reuses the **same** `Product` entity, `AppDbContext`, `IProductService`/`ProductService`, and SQL Server provider as the Minimal API project — only the endpoint/transport layer differs.

- **One endpoint class per use case**, following the REPR (Request–Endpoint–Response) pattern:
  - `CreateProductEndpoint : Endpoint<CreateProductRequest, ProductResponse>`
  - `GetProductEndpoint : Endpoint<GetProductRequest, ProductResponse>`
  - `GetAllProductsEndpoint : EndpointWithoutRequest<List<ProductResponse>>`
  - `UpdateProductEndpoint : Endpoint<UpdateProductRequest, ProductResponse>`
  - `DeleteProductEndpoint : Endpoint<DeleteProductRequest>`
- **Constructor injection** is used throughout (`IProductService` is injected via each endpoint's constructor), not property injection.
- **Startup wiring**:
  ```csharp
  builder.Services.AddFastEndpoints();
  builder.Services.AddScoped<IProductService, ProductService>();
  // ...
  app.UseFastEndpoints();
  ```
- **Binding** is automatic: route parameters (`{id}`) and JSON body properties are matched onto the request DTO by name with no manual model-binding code required.
- **Responses** are sent through the `Send` property:
  ```csharp
  await Send.OkAsync(response);                                   // 200
  await Send.CreatedAtAsync<GetProductEndpoint>(new { id }, dto);  // 201 + Location header
  await Send.NoContentAsync();                                     // 204
  await Send.NotFoundAsync();                                      // 404
  ```
- **Validation** is implemented with a dedicated `FluentValidation` validator per request type (e.g. `CreateProductValidator : Validator<CreateProductRequest>`), auto-discovered by FastEndpoints without manual registration. Data Annotations were **not** mixed in alongside it, since FastEndpoints only honors one validation strategy per DTO.
- **No Swagger** — `FastEndpoints.Swagger` was deliberately not installed, and no Swagger middleware is configured.

Both projects were verified to expose equivalent routes, status codes, and response bodies using the same Postman collection against each base URL in turn.

---

## Testing with Postman

The `/postman` folder contains:

- **`ProductApi.postman_collection.json`** — one request per CRUD operation, per project, including at least one valid and one invalid example (to exercise both the success and `400`/`404` paths).
- **`ProductApi.postman_environment.json`** — an environment with a `baseUrl` variable, swapped between the Minimal API's HTTPS URL and the FastEndpoints project's HTTPS URL to run the same requests against both.

Import both files into Postman, select the environment, point `baseUrl` at whichever project you're testing, and run the collection.

---

## HTTPS & Local Certificates

Both projects run on HTTPS using the ASP.NET Core development certificate. One-time setup per machine:

```bash
dotnet dev-certs https --trust
```

If Postman or a browser reports the certificate as untrusted:

```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

...then close and reopen the browser/tool so its certificate-trust cache refreshes.

Production certificate paths are safe to keep in `appsettings.json`; certificate **passwords** are not — those are supplied via User Secrets in development and via environment variables or a managed secret store (e.g. Azure Key Vault) in production, never committed to source control.

---

## Minimal API vs. FastEndpoints vs. Controllers

| Criterion | Minimal APIs | FastEndpoints | Controllers |
|---|---|---|---|
| **Endpoint registration** | Inline lambda delegates mapped directly on `WebApplication` (`app.MapPost(...)`) | Endpoint classes auto-discovered from the assembly at startup via `AddFastEndpoints()` | Classes decorated with routing attributes, discovered via `AddControllers()`/`MapControllers()` |
| **Dependency injection** | Parameters resolved directly in the delegate's signature | Constructor (or property) injection into each endpoint class | Constructor injection into the controller class |
| **Request binding** | Manual or attribute-based (`[FromBody]`, `[FromRoute]`, etc.) per parameter | Automatic, convention-based binding across route/query/body/headers into one DTO | Model binding across the same sources, driven by MVC's binder pipeline |
| **Validation** | Manual (hand-written checks, or a library wired in yourself) | Built-in FluentValidation integration, auto-discovered per request type | Manual, or via Data Annotations / FluentValidation wired into MVC's filter pipeline |
| **Filters/processors** | `IEndpointFilter` | Pre/Post-processors specific to FastEndpoints | Action filters, a mature and long-standing extensibility model |
| **Response handling** | `Results.*` / `TypedResults.*` helpers | `Send.*` methods on each endpoint | `ActionResult`/`IActionResult` return types |
| **Fewest dependencies** | **Minimal APIs** — ships entirely inside the ASP.NET Core shared framework, no extra NuGet package | Requires the third-party `FastEndpoints` package (and FluentValidation) | Ships inside ASP.NET Core, but pulls in the larger MVC subsystem |
| **Strongest built-in convention** | **FastEndpoints** — one class per use case is enforced by the library's design, imposing structure by default | Controllers also impose real structure (one class per resource), but less prescriptive at the individual-action level | — |
| **Third-party dependency risk (FastEndpoints)** | — | Introduces a dependency on a community-maintained package outside Microsoft's release cadence — upgrade timing, long-term maintenance, and breaking changes are the library maintainers' decisions, not Microsoft's | — |

**Which style for which scenario?**

- **A small service** → **Minimal APIs**. Fewest moving parts, no extra package to learn or justify, and the whole surface area of a small CRUD API fits comfortably in a `Program.cs` (or a couple of endpoint-mapping extension methods).
- **A vertical-slice application** → **FastEndpoints**. Its one-class-per-use-case design *is* the vertical-slice pattern; the library's conventions and DI model actively reinforce that architecture rather than requiring extra discipline to maintain it.
- **An existing controller-based system** → **Controllers**. Consistency with the existing codebase, the team's existing familiarity, and the surrounding tooling/ecosystem (established testing patterns, filters, existing conventions) outweigh any structural or performance argument for switching styles mid-project.

This conclusion isn't based on raw performance alone — Minimal APIs and FastEndpoints are both very fast, and the gap to Controllers is rarely the deciding factor in practice. What actually varies between the three is **maintainability** (how much structure is enforced for you vs. left to team discipline), **team familiarity** (Controllers have the longest institutional track record), **testing** (all three are testable, but FastEndpoints ships first-class integration-testing helpers out of the box), **ecosystem support** (Controllers have the deepest third-party tooling history; FastEndpoints' ecosystem is smaller and community-driven), and **project size** (a one-file Minimal API doesn't need FastEndpoints' structure, and a large multi-team codebase benefits from more convention than Minimal APIs impose on their own).

---

## Final Recommendation

For this specific assignment's scope — a small, single-resource CRUD API — **Minimal APIs** were sufficient and required the least ceremony. For a project expected to grow into many resources maintained by multiple contributors, **FastEndpoints** is the stronger long-term choice: it delivers Minimal-API-level performance while enforcing the same vertical-slice discipline a growing codebase eventually needs anyway, without requiring the heavier MVC/Controllers subsystem.

---

## License

This project is licensed under the [MIT License](./LICENSE).
