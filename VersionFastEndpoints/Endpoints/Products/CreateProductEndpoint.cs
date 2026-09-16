using FastEndpoints;
using ProductShared.DTOs;
using ProductShared.Services;

namespace FastEndpointsVersion.Endpoints.Products;

public class CreateProductEndpoint : Endpoint<ProductRequestDto, ProductResponseDto>
{
    private readonly IProductService _service;

    public CreateProductEndpoint(IProductService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Post("/api/products");
        AllowAnonymous();
    }

    public override async Task HandleAsync(
        ProductRequestDto req,
        CancellationToken ct)
    {
        var created = await _service.CreateAsync(req, ct);

        await Send.CreatedAtAsync<GetProductByIdEndpoint>(
            routeValues: new { id = created.Id },
            responseBody: created,
            cancellation: ct);
    }
}