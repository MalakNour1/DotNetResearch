using FastEndpoints;
using ProductShared.Services;

namespace FastEndpointsVersion.Endpoints.Products;

public class DeleteProductRequest
{
    public int Id { get; set; }
}

public class DeleteProductEndpoint : Endpoint<DeleteProductRequest>
{
    private readonly IProductService _service;

    public DeleteProductEndpoint(IProductService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Delete("/api/products/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(
        DeleteProductRequest req,
        CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(req.Id, ct);

        if (!deleted)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}