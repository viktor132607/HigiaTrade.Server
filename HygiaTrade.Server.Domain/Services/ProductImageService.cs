using HygiaTrade.Common.Requests.Image;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;

namespace HygiaTrade.Domain.Services;

public interface IProductImageService
{
    Task<List<Image>> AddAsync(
        Product product,
        IEnumerable<CreateImageRequest> requests);

    Task ReplaceAsync(
        Product product,
        IEnumerable<UpdateImageRequest> requests);

    Task DeleteAllAsync(Product product);
}

public sealed class ProductImageService(
    IImageRepository imageRepository)
    : IProductImageService
{
    public async Task<List<Image>> AddAsync(
        Product product,
        IEnumerable<CreateImageRequest> requests)
    {
        List<Image> images = [];

        foreach (CreateImageRequest request in requests)
        {
            var image = new Image
            {
                Uri = request.Uri,
                ProductId = product.Id
            };

            images.Add(image);
            await imageRepository.AddAsync(image);
        }

        return images;
    }

    public async Task ReplaceAsync(
        Product product,
        IEnumerable<UpdateImageRequest> requests)
    {
        await DeleteAllAsync(product);
        product.SecondaryImages.Clear();

        foreach (UpdateImageRequest request in requests)
        {
            var image = new Image
            {
                Uri = request.Uri,
                ProductId = product.Id
            };

            product.SecondaryImages.Add(image);
            await imageRepository.AddAsync(image);
        }
    }

    public async Task DeleteAllAsync(Product product)
    {
        foreach (Image image
                 in product.SecondaryImages.ToList())
        {
            await imageRepository.DeleteAsync(image.Id);
        }
    }
}
