using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class HomeSlideshowPayloadNormalizerTests
{
    private readonly HomeSlideshowPayloadNormalizer normalizer = new();

    [Fact]
    public void Normalize_RejectsEmptySlideList()
    {
        var payload = new HomeSlideshowPayload();

        HomeSlideshowServiceException ex =
            Assert.Throws<HomeSlideshowServiceException>(
                () => normalizer.Normalize(payload));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(
            "At least one slideshow item is required.",
            ex.Message);
    }

    [Fact]
    public void Normalize_RejectsMoreThanTwentySlides()
    {
        var payload = new HomeSlideshowPayload
        {
            Slides = Enumerable.Range(0, 21)
                .Select(_ => new HomeSlideDto())
                .ToList()
        };

        HomeSlideshowServiceException ex =
            Assert.Throws<HomeSlideshowServiceException>(
                () => normalizer.Normalize(payload));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(
            "A maximum of 20 slideshow items is supported.",
            ex.Message);
    }

    [Fact]
    public void Normalize_AssignsMissingIdsAndSequentialOrder()
    {
        var payload = new HomeSlideshowPayload
        {
            Slides =
            [
                new HomeSlideDto { Id = " " },
                new HomeSlideDto { Id = "fixed" }
            ]
        };

        normalizer.Normalize(payload);

        Assert.False(
            string.IsNullOrWhiteSpace(
                payload.Slides[0].Id));

        Assert.NotEqual(
            "fixed",
            payload.Slides[0].Id);

        Assert.Equal("fixed", payload.Slides[1].Id);
        Assert.Equal(0, payload.Slides[0].Order);
        Assert.Equal(1, payload.Slides[1].Order);
    }

    [Fact]
    public void Normalize_TrimsAllTextFields()
    {
        var slide = new HomeSlideDto
        {
            Id = "id",
            TitleBg = "  bg  ",
            TitleEn = "  en  ",
            EyebrowBg = "  ebg  ",
            EyebrowEn = "  een  ",
            BadgeBg = "  bbg  ",
            BadgeEn = "  ben  ",
            NoteBg = "  nbg  ",
            NoteEn = "  nen  ",
            CtaBg = "  cbg  ",
            CtaEn = "  cen  ",
            CtaUrl = "  /products  ",
            Image = "  image  ",
            Accent = "  accent  "
        };

        var payload = new HomeSlideshowPayload
        {
            Slides = [slide]
        };

        normalizer.Normalize(payload);

        Assert.Equal("bg", slide.TitleBg);
        Assert.Equal("en", slide.TitleEn);
        Assert.Equal("ebg", slide.EyebrowBg);
        Assert.Equal("een", slide.EyebrowEn);
        Assert.Equal("bbg", slide.BadgeBg);
        Assert.Equal("ben", slide.BadgeEn);
        Assert.Equal("nbg", slide.NoteBg);
        Assert.Equal("nen", slide.NoteEn);
        Assert.Equal("cbg", slide.CtaBg);
        Assert.Equal("cen", slide.CtaEn);
        Assert.Equal("/products", slide.CtaUrl);
        Assert.Equal("image", slide.Image);
        Assert.Equal("accent", slide.Accent);
    }

    [Fact]
    public void Normalize_TruncatesEachFieldToConfiguredLimit()
    {
        var slide = new HomeSlideDto
        {
            Id = "id",
            TitleBg = new string('a', 121),
            TitleEn = new string('b', 121),
            EyebrowBg = new string('c', 121),
            EyebrowEn = new string('d', 121),
            BadgeBg = new string('e', 81),
            BadgeEn = new string('f', 81),
            NoteBg = new string('g', 301),
            NoteEn = new string('h', 301),
            CtaBg = new string('i', 81),
            CtaEn = new string('j', 81),
            CtaUrl = new string('k', 501),
            Image = new string('l', 2001),
            Accent = new string('m', 201)
        };

        var payload = new HomeSlideshowPayload
        {
            Slides = [slide]
        };

        normalizer.Normalize(payload);

        Assert.Equal(120, slide.TitleBg.Length);
        Assert.Equal(120, slide.TitleEn.Length);
        Assert.Equal(120, slide.EyebrowBg.Length);
        Assert.Equal(120, slide.EyebrowEn.Length);
        Assert.Equal(80, slide.BadgeBg.Length);
        Assert.Equal(80, slide.BadgeEn.Length);
        Assert.Equal(300, slide.NoteBg.Length);
        Assert.Equal(300, slide.NoteEn.Length);
        Assert.Equal(80, slide.CtaBg.Length);
        Assert.Equal(80, slide.CtaEn.Length);
        Assert.Equal(500, slide.CtaUrl.Length);
        Assert.Equal(2000, slide.Image.Length);
        Assert.Equal(200, slide.Accent.Length);
    }

    [Fact]
    public void Normalize_ConvertsNullTextValuesToEmptyStrings()
    {
        var slide = new HomeSlideDto
        {
            Id = "id",
            TitleBg = null!,
            TitleEn = null!,
            EyebrowBg = null!,
            EyebrowEn = null!,
            BadgeBg = null!,
            BadgeEn = null!,
            NoteBg = null!,
            NoteEn = null!,
            CtaBg = null!,
            CtaEn = null!,
            CtaUrl = null!,
            Image = null!,
            Accent = null!
        };

        var payload = new HomeSlideshowPayload
        {
            Slides = [slide]
        };

        normalizer.Normalize(payload);

        Assert.Equal(string.Empty, slide.TitleBg);
        Assert.Equal(string.Empty, slide.TitleEn);
        Assert.Equal(string.Empty, slide.EyebrowBg);
        Assert.Equal(string.Empty, slide.EyebrowEn);
        Assert.Equal(string.Empty, slide.BadgeBg);
        Assert.Equal(string.Empty, slide.BadgeEn);
        Assert.Equal(string.Empty, slide.NoteBg);
        Assert.Equal(string.Empty, slide.NoteEn);
        Assert.Equal(string.Empty, slide.CtaBg);
        Assert.Equal(string.Empty, slide.CtaEn);
        Assert.Equal(string.Empty, slide.CtaUrl);
        Assert.Equal(string.Empty, slide.Image);
        Assert.Equal(string.Empty, slide.Accent);
    }
}
