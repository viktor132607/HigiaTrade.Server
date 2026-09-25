using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class EmailContentFormatterTests
{
    [Fact]
    public void Encode_HtmlEncodesInput()
    {
        Assert.Equal(
            "&lt;b&gt;&amp;&quot;",
            EmailContentFormatter.Encode("<b>&\""));
    }

    [Fact]
    public void NormalizeSubject_RemovesLineBreaksAndTrims()
    {
        Assert.Equal(
            "Hello World Again",
            EmailContentFormatter.NormalizeSubject(
                "  Hello\rWorld\nAgain  "));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n")]
    public void NormalizeSubject_UsesFallback_WhenBlank(
        string value)
    {
        Assert.Equal(
            "Contact enquiry",
            EmailContentFormatter.NormalizeSubject(value));
    }

    [Fact]
    public void FormatMoney_UsesBulgarianCurrencyCulture()
    {
        string value =
            EmailContentFormatter.FormatMoney(12.34m);

        Assert.Contains("12", value);
        Assert.Contains("лв", value);
    }
}
