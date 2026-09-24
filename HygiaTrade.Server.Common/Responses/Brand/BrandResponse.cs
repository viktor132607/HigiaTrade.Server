namespace HygiaTrade.Common.Responses.Brand;

public sealed record BrandResponse(
    Guid Id,
    string Name,
    string? ThumbnailImageUrl,
    string? Description,
    int ProductCount);
