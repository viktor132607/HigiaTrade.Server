using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class HomeSlideshowDefaultsTests
{
    [Fact]
    public void Create_ReturnsThreeOrderedActiveDefaultSlides()
    {
        var defaults = new HomeSlideshowDefaults();

        var result = defaults.Create();

        Assert.Equal(3, result.Slides.Count);

        Assert.Collection(
            result.Slides,
            first =>
            {
                Assert.Equal("1", first.Id);
                Assert.Equal(0, first.Order);
                Assert.True(first.IsActive);
                Assert.Equal(
                    "Почистващи препарати",
                    first.TitleBg);
                Assert.Equal("/products", first.CtaUrl);
            },
            second =>
            {
                Assert.Equal("2", second.Id);
                Assert.Equal(1, second.Order);
                Assert.True(second.IsActive);
                Assert.Equal(
                    "Перилни препарати",
                    second.TitleBg);
            },
            third =>
            {
                Assert.Equal("3", third.Id);
                Assert.Equal(2, third.Order);
                Assert.True(third.IsActive);
                Assert.Equal(
                    "За бизнеса и офиса",
                    third.TitleBg);
            });
    }

    [Fact]
    public void Create_ReturnsFreshPayloadEachTime()
    {
        var defaults = new HomeSlideshowDefaults();

        var first = defaults.Create();
        var second = defaults.Create();

        Assert.NotSame(first, second);
        Assert.NotSame(first.Slides, second.Slides);

        first.Slides[0].TitleBg = "Changed";

        Assert.Equal(
            "Почистващи препарати",
            second.Slides[0].TitleBg);
    }
}
