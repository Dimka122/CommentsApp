using Comments.Application.Search;
using Xunit;

namespace Comments.UnitTests;

public class PlainTextTests
{
    [Fact] public void Strips_tags_and_decodes_entities() =>
        Assert.Equal("Hi there you <3 & more", PlainText.FromHtml("<strong>Hi</strong> there<br />you &lt;3 &amp; more"));

    [Fact] public void Does_not_split_words_on_inline_tags() =>
        Assert.Equal("hello", PlainText.FromHtml("<i>he</i>llo"));

    [Fact] public void Escaped_script_stays_text_not_markup() =>
        Assert.Equal("<script>alert(1)</script>", PlainText.FromHtml("&lt;script&gt;alert(1)&lt;/script&gt;"));
}
