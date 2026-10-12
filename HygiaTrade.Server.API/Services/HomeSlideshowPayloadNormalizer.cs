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
        payload.SlideDurationSeconds = payload.SlideDurationSeconds <= 0
            ? 5
            : Math.Clamp(payload.SlideDurationSeconds, 1, 60);

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
        slide.ImagePositionY = Math.Clamp(slide.ImagePositionY, 0, 100);
        slide.ImageLightening = Math.Clamp(slide.ImageLightening ?? 100, 0, 100);
        slide.Accent = Limit(slide.Accent, 200);

        slide.Elements = (slide.Elements ?? [])
            .Where(element => element is not null)
            .Take(30)
            .Select(element => new HomeSlideOverlayElement
            {
                Id = Limit(element.Id, 64),
                Kind = element.Kind == "button" ? "button" : "text",
                TextBg = Limit(element.TextBg, 300),
                TextEn = Limit(element.TextEn, 300),
                Url = NormalizeUrl(element.Url),
                Color = NormalizeColor(element.Color, "#0f172a"),
                Background = NormalizeColor(element.Background, "#0f172a"),
                FontSize = Math.Clamp(element.FontSize, 12, 96)
            })
            .Where(element => element.Id.Length > 0)
            .DistinctBy(element => element.Id)
            .ToList();

        var allowedElements = new HashSet<string>(StringComparer.Ordinal)
        {
            "eyebrow", "title", "badge", "note", "cta"
        };
        foreach (HomeSlideOverlayElement element in slide.Elements)
        {
            allowedElements.Add("extra:" + element.Id);
        }

        slide.Positions = (slide.Positions ?? new Dictionary<string, SlideElementPosition>())
            .Where(entry => allowedElements.Contains(entry.Key) && entry.Value is not null)
            .ToDictionary(
                entry => entry.Key,
                entry => new SlideElementPosition
                {
                    X = double.IsFinite(entry.Value.X) ? Math.Clamp(entry.Value.X, 0d, 100d) : 0d,
                    Y = double.IsFinite(entry.Value.Y) ? Math.Clamp(entry.Value.Y, 0d, 100d) : 0d,
                    Scale = double.IsFinite(entry.Value.Scale) ? Math.Clamp(entry.Value.Scale, 0.5d, 2.5d) : 1d
                },
                StringComparer.Ordinal);
    }

    private static string NormalizeColor(string? color, string fallback) =>
        !string.IsNullOrWhiteSpace(color) &&
        System.Text.RegularExpressions.Regex.IsMatch(color, "^#[a-fA-F0-9]{6}$")
            ? color
            : fallback;

    private static string NormalizeUrl(string? url)
    {
        string value = Limit(url, 500);
        return value.StartsWith("/") && !value.StartsWith("//") ? value : "/products";
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
