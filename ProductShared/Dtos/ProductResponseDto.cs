namespace ProductShared.DTOs;

public record ProductResponseDto(int Id, string Name, decimal Price, DateTime CreatedAtUtc);