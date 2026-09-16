using FastEndpoints;
using ProductShared.DTOs;
using ProductShared.Services;

namespace FastEndpointsVersion.Endpoints.Products;

/*
Two cases where you need an extra request class:

Case A - Data comes from the route (not a JSON body)
Example: GET /api/products/{id}

Case B - Data comes from the route AND the body together
Example: PUT /api/products/{id} with a JSON body.

id comes from the route.
Name and Price come from the JSON body.
FastEndpoints can only bind to one request type, so we make a class that has all three properties: UpdateProductRequest { Id, Name, Price }.
 */
public class GetProductByIdRequest
{
    public int Id { get; set; }
}

public class GetProductByIdEndpoint
    : Endpoint<GetProductByIdRequest, ProductResponseDto>
{
    private readonly IProductService _service;

    public GetProductByIdEndpoint(IProductService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Get("/api/products/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(
        GetProductByIdRequest req,
        CancellationToken ct)
    {
        var product = await _service.GetByIdAsync(req.Id, ct);

        if (product is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(product, ct);
    }
}