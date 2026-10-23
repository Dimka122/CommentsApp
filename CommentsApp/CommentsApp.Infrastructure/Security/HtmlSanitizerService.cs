using CommentsApp.CommentsApp.Application.Interfaces;
using Ganss.Xss;
using System.Text.RegularExpressions;

namespace CommentsApp.CommentsApp.Infrastructure.Security;

public class HtmlSanitizerService : IHtmlSanitizerService
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.UnionWith(["a", "code", "i", "strong"]);
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.UnionWith(["href", "title"]);
        _sanitizer.AllowedSchemes.UnionWith(["http", "https"]);
    }

    public string Sanitize(string rawHtml)
    {
        var clean = _sanitizer.Sanitize(rawHtml ?? string.Empty);
        return EnsureBalanced(clean);
    }

    // Простая проверка парности тегов (валидный XHTML): незакрытые — экранируем удалением
    private static string EnsureBalanced(string html)
    {
        var stack = new Stack<string>();
        var matches = System.Text.RegularExpressions.Regex.Matches(
            html, @"<(/?)(a|code|i|strong)([^>]*)>");

        foreach (Match m in matches)
        {
            var tag = m.Groups[2].Value.ToLower();
            if (m.Groups[1].Value == "/")
            {
                if (stack.Count == 0 || stack.Pop() != tag)
                    return System.Net.WebUtility.HtmlEncode(html); // невалидно — экранируем целиком
            }
            else if (!m.Value.EndsWith("/>"))
                stack.Push(tag);
        }
        return stack.Count == 0 ? html : System.Net.WebUtility.HtmlEncode(html);
    }
}
