using HygiaTrade.API.Controllers;

namespace HygiaTrade.API.Services;

public interface IHomeSlideshowService
{
    Task<HomeSlideshowPayload> GetAsync();

    Task<HomeSlideshowPayload> UpdateAsync(
        HomeSlideshowPayload payload);
}

public sealed class HomeSlideshowServiceException(
    int statusCode,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class HomeSlideshowService(
    IHomeSlideshowStore store,
    IHomeSlideshowPayloadNormalizer normalizer,
    IHomeSlideshowDefaults defaults)
    : IHomeSlideshowService
{
    public async Task<HomeSlideshowPayload> GetAsync()
    {
        await store.EnsureAsync();

        return await store.ReadAsync()
            ?? defaults.Create();
    }

    public async Task<HomeSlideshowPayload> UpdateAsync(
        HomeSlideshowPayload payload)
    {
        normalizer.Normalize(payload);

        await store.EnsureAsync();
        await store.WriteAsync(payload);

        return payload;
    }
}
