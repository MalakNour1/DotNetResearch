using Microsoft.EntityFrameworkCore;
using ProductShared.Data;
using ProductShared.DTOs;
using ProductShared.Services;

var builder = WebApplication.CreateBuilder(args);

/* 
1. Register the DbContext with Scoped lifetime.
Why "scoped"?
- DbContext is not thread-safe, so Singleton is out (concurrent requests would corrupt its change tracker).
- Transient would create a new DbContext on every resolve,losing tracking consistency within a single request.
- Scoped = one DbContext per HTTP request. Perfect fit.
*/
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

/*
2. Register the ProductService with Scoped lifetime.
Why "scoped"?
- It uses AppDbContext, and AppDbContext is Scoped.
- A Singleton service would hold onto a Scoped DbContext forever, which is called a "captive dependency" -> crashes / shared state bugs.
- Rule of thumb: a service should never live longer than its dependencies.
- So: Scoped keeps service + DbContext alive for the same request only.
*/
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();


//Create a product
app.MapPost("/api/products", async (
    ProductRequestDto dto,
    IProductService service,
    CancellationToken ct) =>
{
    // Basic validation (FastEndpoints will do this more elegantly later)
    if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0)
        return Results.BadRequest(new { error = "Name is required and Price must be greater than 0." });

    var created = await service.CreateAsync(dto, ct);

    // 201 with Location header pointing to GET-by-id
    return Results.Created($"/api/products/{created.Id}", created);
});

//Retrieve all products
app.MapGet("/api/products", async (
    IProductService service,
    CancellationToken ct) =>
{
    var products = await service.GetAllAsync(ct);
    return Results.Ok(products);
});

//Retrieve a product
app.MapGet("/api/products/{id:int}", async (
    int id,
    IProductService service,
    CancellationToken ct) =>
{
    var product = await service.GetByIdAsync(id, ct);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

//Update
app.MapPut("/api/products/{id:int}", async (
    int id,
    ProductRequestDto dto,
    IProductService service,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0)
        return Results.BadRequest(new { error = "Name is required and Price must be greater than 0." });

    var updated = await service.UpdateAsync(id, dto, ct);
    return updated ? Results.NoContent() : Results.NotFound();
});

//Delete
app.MapDelete("/api/products/{id:int}", async (
    int id,
    IProductService service,
    CancellationToken ct) =>
{
    var deleted = await service.DeleteAsync(id, ct);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();