using FastEndpoints;
using ProductShared.DTOs;
using ProductShared.Services;

namespace FastEndpointsVersion.Endpoints.Products;

public class GetAllProductsEndpoint
    : EndpointWithoutRequest<List<ProductResponseDto>>
{
    private readonly IProductService _service;

    public GetAllProductsEndpoint(IProductService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Get("/api/products");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var products = await _service.GetAllAsync(ct);

        await Send.OkAsync(products, ct);
    }
}