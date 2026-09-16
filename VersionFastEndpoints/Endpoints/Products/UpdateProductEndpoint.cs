using FastEndpoints;
using ProductShared.DTOs;
using ProductShared.Services;

namespace FastEndpointsVersion.Endpoints.Products;

public class UpdateProductRequest
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
}

public class UpdateProductEndpoint : Endpoint<UpdateProductRequest>
{
    private readonly IProductService _service;

    public UpdateProductEndpoint(IProductService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Put("/api/products/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(
        UpdateProductRequest req,
        CancellationToken ct)
    {
        var dto = new ProductRequestDto(
            req.Name,
            req.Price);

        var updated = await _service.UpdateAsync(
            req.Id,
            dto,
            ct);

        if (!updated)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}