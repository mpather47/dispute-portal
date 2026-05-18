using DisputePortal.Api.Helpers;
using Xunit;

namespace DisputePortal.Tests.Helpers;

public class InputSanitizerTests
{
    [Theory]
    [InlineData("Hello World", "Hello World")]
    [InlineData("  trimmed  ", "trimmed")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Sanitize_PlainText_ReturnsUnchanged(string? input, string expected)
    {
        Assert.Equal(expected, InputSanitizer.Sanitize(input));
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>", "alert('xss')")]
    [InlineData("<b>bold</b>", "bold")]
    [InlineData("<img src=x onerror=alert(1)>", "")]
    [InlineData("<a href='evil.com'>click</a>", "click")]
    [InlineData("<p>para</p><br/>line", "paraline")]
    public void Sanitize_HtmlTags_StripsAllTags(string input, string expected)
    {
        Assert.Equal(expected, InputSanitizer.Sanitize(input));
    }

    [Theory]
    [InlineData("&lt;script&gt;alert(1)&lt;/script&gt;", "alert(1)")]
    [InlineData("&amp;", "&")]
    [InlineData("&lt;b&gt;text&lt;/b&gt;", "text")]
    public void Sanitize_HtmlEncodedEntities_DecodesAndStrips(string input, string expected)
    {
        Assert.Equal(expected, InputSanitizer.Sanitize(input));
    }

    [Fact]
    public void Sanitize_MixedContent_PreservesTextPortions()
    {
        var result = InputSanitizer.Sanitize("Hello <b>world</b>, how are <em>you</em>?");
        Assert.Equal("Hello world, how are you?", result);
    }

    [Fact]
    public void Sanitize_NestedTags_RemovesAll()
    {
        var result = InputSanitizer.Sanitize("<div><span><b>deep</b></span></div>");
        Assert.Equal("deep", result);
    }
}
