using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductShared.Data;
using ProductShared.DTOs;
using ProductShared.Models;

namespace ProductShared.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(AppDbContext db, ILogger<ProductService> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    public async Task<ProductResponseDto> CreateAsync(ProductRequestDto dto, CancellationToken ct)
    {
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Products.Add(product);

        // Pass CT so EF can cancel if the client disconnects mid-save
        await _db.SaveChangesAsync(ct);

        // Structured logging: parameters become queryable fields in log tools
        _logger.LogInformation(
            "Created product {ProductId} with name {ProductName} and price {ProductPrice}",
            product.Id, product.Name, product.Price);

        return MapToDto(product);
    }
    
    public async Task<List<ProductResponseDto>> GetAllAsync(CancellationToken ct)
    {
        /*
        AsNoTracking: this is a readonly query. We don't plan to modify these entities.
        Tracking them wastes memory and CPU because EF would snapshot every property.
        For read-only endpoints, AsNoTracking is significantly faster.
        */
        return await _db.Products
            .AsNoTracking()
            .Select(p => new ProductResponseDto(p.Id, p.Name, p.Price, p.CreatedAtUtc))
            .ToListAsync(ct);
    }
    
    public async Task<ProductResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
        {
            // Log at Warning level this is an expected but notable event
            _logger.LogWarning("Product {ProductId} not found", id);
            return null;
        }

        return MapToDto(product);
    }
    
    public async Task<bool> UpdateAsync(int id, ProductRequestDto dto, CancellationToken ct)
    {
        // Here we do NOT use AsNoTracking because we're going to modify this entity and save it. Tracking is required.
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
        {
            _logger.LogWarning("Update failed. Product {ProductId} not found", id);
            return false;
        }

        product.Name = dto.Name;
        product.Price = dto.Price;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Updated product {ProductId}", id);
        return true;
    }
    
    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
        {
            _logger.LogWarning("Delete failed. Product {ProductId} not found", id);
            return false;
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted product {ProductId}", id);
        return true;
    }

    // Helper: never expose the EF entity directly to the API
    private static ProductResponseDto MapToDto(Product p) =>
        new(p.Id, p.Name, p.Price, p.CreatedAtUtc);
}