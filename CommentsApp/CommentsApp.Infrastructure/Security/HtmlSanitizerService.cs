using CommentsApp.Application.Interfaces;
using CommentsApp.CommentsApp.Application.Interfaces;
using Ganss.Xss;

namespace CommentsApp.Infrastructure.Security;

public class HtmlSanitizerService : IHtmlSanitizerService
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();

        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.UnionWith(["a", "i", "b", "strong", "em", "code", "pre"]);

        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("rel");

        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");
    }

    public string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";

        var result = _sanitizer.Sanitize(html);

        // nofollow/noopener для всех исходящих ссылок
        return result.Replace("<a ", "<a rel=\"nofollow noopener noreferrer\" ",
            StringComparison.OrdinalIgnoreCase);
    }
}
