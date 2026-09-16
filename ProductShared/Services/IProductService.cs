using ProductShared.DTOs;
namespace ProductShared.Services;

public interface IProductService
{
    Task<ProductResponseDto> CreateAsync(ProductRequestDto dto, CancellationToken ct);
    Task<List<ProductResponseDto>> GetAllAsync(CancellationToken ct);
    Task<ProductResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> UpdateAsync(int id, ProductRequestDto dto, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}