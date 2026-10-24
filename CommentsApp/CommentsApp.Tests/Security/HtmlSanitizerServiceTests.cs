using CommentsApp.Infrastructure.Security;
using Xunit;

namespace CommentsApp.Tests.Security;

public class HtmlSanitizerServiceTests
{
    private readonly HtmlSanitizerService _sanitizer = new();

    [Theory]
    [InlineData("<scr<script>ipt>alert(1)</script>", "alert(1)")]           // вложенный тег
    [InlineData("<a href=\"javascript:alert(1)\">x</a>", "javascript")]     // плохая схема
    [InlineData("<a href=\"jav&#x0A;ascript:alert(1)\">x</a>", "javascript")] // entity-обфускация
    [InlineData("<img src=x onerror=alert(1)>", "onerror")]                 // не-whitelist тег
    [InlineData("<b onclick=\"alert(1)\">hi</b>", "onclick")]               // событие в атрибуте
    [InlineData("<!-- comment --><i>ok</i>", "comment")]                    // HTML-комментарий
    [InlineData("<a href=\"https://ok.com\" target=\"_blank\">x</a>", "target")] // target удалён
    public void Sanitize_Removes_Dangerous_Content(string input, string mustNotContain)
    {
        var result = _sanitizer.Sanitize(input);

        Assert.DoesNotContain(mustNotContain, result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<b>привет</b>", "<b>привет</b>")]      // разрешённый тег остаётся
    [InlineData("<i>ok</i>", "<i>ok</i>")]
    [InlineData("<code>x = 1</code>", "<code>x = 1</code>")]
    [InlineData("просто текст", "просто текст")]         // текст без HTML не трогается
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Sanitize_Keeps_Allowed_Content(string? input, string expected)
    {
        var result = _sanitizer.Sanitize(input!);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Sanitize_Link_Keeps_Href_And_Adds_Rel()
    {
        var result = _sanitizer.Sanitize("<a href=\"https://example.com\">сайт</a>");

        Assert.Contains("href=\"https://example.com\"", result);
        Assert.Contains("nofollow", result);
    }
}
