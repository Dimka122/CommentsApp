using Comments.Application.Html;
using Xunit;

namespace Comments.UnitTests;

public class HtmlSanitizerTests
{
    private readonly HtmlSanitizer _s = new();

    [Fact] public void Allowed_tags_pass() =>
        Assert.Equal("<strong>hi <i>there</i></strong> <code>x</code>", _s.Sanitize("<strong>hi <i>there</i></strong> <code>x</code>").Html);

    [Fact] public void Script_is_escaped_not_executed()
    {
        var r = _s.Sanitize("<script>alert(1)</script>");
        Assert.True(r.IsValid);
        Assert.DoesNotContain("<script", r.Html);
        Assert.Contains("&lt;script&gt;", r.Html);
    }

    [Fact] public void Unclosed_tag_is_invalid() => Assert.False(_s.Sanitize("<i>a").IsValid);
    [Fact] public void Wrong_nesting_is_invalid() => Assert.False(_s.Sanitize("<i><strong>a</i></strong>").IsValid);
    [Fact] public void Stray_closing_tag_is_invalid() => Assert.False(_s.Sanitize("a</strong>").IsValid);

    [Fact] public void Anchor_with_https_href_and_title_passes()
    {
        var r = _s.Sanitize("<a href=\"https://example.com/?a=1&b=2\" title=\"t\">x</a>");
        Assert.True(r.IsValid);
        Assert.Contains("href=\"https://example.com/?a=1&amp;b=2\"", r.Html);
        Assert.Contains("title=\"t\"", r.Html);
    }

    [Fact] public void Javascript_href_is_rejected() => Assert.False(_s.Sanitize("<a href=\"javascript:alert(1)\">x</a>").IsValid);
    [Fact] public void Event_handler_attribute_is_rejected() => Assert.False(_s.Sanitize("<a href=\"https://e.com\" onclick=\"x()\">x</a>").IsValid);
    [Fact] public void Anchor_without_href_is_rejected() => Assert.False(_s.Sanitize("<a title=\"t\">x</a>").IsValid);

    [Fact] public void Newlines_become_br() => Assert.Equal("a<br />b", _s.Sanitize("a\nb").Html);
    [Fact] public void Special_chars_are_encoded() => Assert.Equal("a &lt; b &amp; c", _s.Sanitize("a < b & c").Html);
}
