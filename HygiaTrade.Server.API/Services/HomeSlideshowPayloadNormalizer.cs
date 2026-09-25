using HygiaTrade.API.Controllers;

namespace HygiaTrade.API.Services;

public interface IHomeSlideshowPayloadNormalizer
{
    void Normalize(HomeSlideshowPayload payload);
}

public sealed class HomeSlideshowPayloadNormalizer
    : IHomeSlideshowPayloadNormalizer
{
    public void Normalize(HomeSlideshowPayload payload)
    {
        if (payload.Slides is null ||
            payload.Slides.Count == 0)
        {
            throw new HomeSlideshowServiceException(
                StatusCodes.Status400BadRequest,
                "At least one slideshow item is required.");
        }

        if (payload.Slides.Count > 20)
        {
            throw new HomeSlideshowServiceException(
                StatusCodes.Status400BadRequest,
                "A maximum of 20 slideshow items is supported.");
        }

        for (
            int index = 0;
            index < payload.Slides.Count;
            index++)
        {
            NormalizeSlide(
                payload.Slides[index],
                index);
        }
    }

    private static void NormalizeSlide(
        HomeSlideDto slide,
        int index)
    {
        if (string.IsNullOrWhiteSpace(slide.Id))
        {
            slide.Id =
                Guid.NewGuid().ToString("N");
        }

        slide.Order = index;
        slide.TitleBg = Limit(slide.TitleBg, 120);
        slide.TitleEn = Limit(slide.TitleEn, 120);
        slide.EyebrowBg = Limit(slide.EyebrowBg, 120);
        slide.EyebrowEn = Limit(slide.EyebrowEn, 120);
        slide.BadgeBg = Limit(slide.BadgeBg, 80);
        slide.BadgeEn = Limit(slide.BadgeEn, 80);
        slide.NoteBg = Limit(slide.NoteBg, 300);
        slide.NoteEn = Limit(slide.NoteEn, 300);
        slide.CtaBg = Limit(slide.CtaBg, 80);
        slide.CtaEn = Limit(slide.CtaEn, 80);
        slide.CtaUrl = Limit(slide.CtaUrl, 500);
        slide.Image = Limit(slide.Image, 2000);
        slide.Accent = Limit(slide.Accent, 200);
    }

    private static string Limit(
        string? value,
        int maxLength)
    {
        string normalized =
            (value ?? string.Empty).Trim();

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
