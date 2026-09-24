namespace HygiaTrade.Common.Requests.Brand;

public sealed record BrandRequest(
    string Name,
    string? ThumbnailImageUrl,
    string? Description);

public sealed record UpdateBrandRequest(
    Guid Id,
    string Name,
    string? ThumbnailImageUrl,
    string? Description);
